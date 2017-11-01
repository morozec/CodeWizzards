using System;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk.Model;

namespace IPA.AStar
{
    public static class Calculator
    {
        /// <summary>
        /// Расчет матрицы распространения
        /// </summary>
        /// <param name="start">Точка, для которой рассчитывается матрица распространения</param>
        /// <param name="goal">Целевая точка. Если null, то матрица распространения рассчитывается от стартовой точки до всех остальных точек сети</param>
        /// <param name="allPoints">Все точки сети</param>
        /// <returns>Матрица распространения</returns>
        private static ExpansionMatrixConteiner GetExpansionMatrix(Point start, Point goal, IEnumerable<Point> allPoints, Wizard self, Game game, double relaxCoeff)
        {
            foreach (var point in allPoints)
            {
                point.CameFromPoint = null;
            }

            var emc = new ExpansionMatrixConteiner
            {
                ExpansionMatrix = new Dictionary<Point, double>(),
                //Path =  new Dictionary<Point, IList<Point>>()
            };

            var closedSet = new HashSet<Point>();
            var openSet = new HashSet<Point> { start };

            start.G = 0d;
            start.H = goal == null ? 0d : start.GetHeuristicCost(goal);

            var pathFound = false;

            while (openSet.Count > 0)
            {
                var x = GetPointWithMinF(openSet);

                if (goal != null && x == goal)
                {
                    pathFound = true;
                    emc.RealGoalPoint = goal;
                    break;
                }
                if (goal != null && x.GetHeuristicCost(goal) < relaxCoeff)
                {
                    pathFound = true;
                    emc.RealGoalPoint = x;
                    break;
                }
                openSet.Remove(x);
                closedSet.Add(x);
                emc.ExpansionMatrix.Add(x, x.G);
                //emc.Path.Add(x, ReconstructPath(x));

                var neighbors = x.GetNeighbors(allPoints);
                foreach (var y in neighbors)
                {
                    if (closedSet.Contains(y)) continue;

                    var tentativeGScore = x.G + x.GetCost(y, self, game);
                    bool tentativeIsBetter;

                    if (!openSet.Contains(y))
                    {
                        openSet.Add(y);
                        tentativeIsBetter = true;
                    }
                    else
                    {
                        tentativeIsBetter = tentativeGScore < y.G;
                    }

                    if (tentativeIsBetter)
                    {
                        y.CameFromPoint = x;
                        y.G = tentativeGScore;
                        y.H = goal == null ? 0d: y.GetHeuristicCost(goal);
                    }
                }
            }

            if (goal != null && !pathFound) throw new Exception("Путь до конечной точки не найден");
            

            return emc;
        }

        /// <summary>
        /// Расчет оптимального пути до целевой точки
        /// </summary>
        /// <param name="start">Стартовая точка пути</param>
        /// <param name="goal">Целевая точка пути</param>
        /// <param name="allPoints">Все точки сети</param>
        /// <returns>Оптимальный путь от стартовой точки до целевой</returns>
        public static IList<Point> GetPath(Point start, Point goal, IEnumerable<Point> allPoints, Wizard self, Game game, double relaxCoeff)
        {
            var emc = GetExpansionMatrix(start, goal, allPoints, self, game, relaxCoeff);
            return ReconstructPath(emc.RealGoalPoint);
        }

        /// <summary>
        /// Получение матриц распространения для набора стартовых точек
        /// </summary>
        /// <param name="startPoints">Набор стартовых точек</param>
        /// <param name="allPoints">Все точки сети</param>
        /// <returns>Матрицы распространения для стартовых точек</returns>
        private static IDictionary<Point, ExpansionMatrixConteiner> GetExpansionMatrices(IEnumerable<Point> startPoints, IEnumerable<Point> allPoints, Wizard self, Game game, double relaxCoeff)
        {
            var result = new Dictionary<Point, ExpansionMatrixConteiner>();
            foreach (var startPoint in startPoints)
            {
                result.Add(startPoint, GetExpansionMatrix(startPoint, null, allPoints, self, game, relaxCoeff));
            }
            return result;
        }

        /// <summary>
        /// Получение точки с минимальным значением после суммирования матриц распространения
        /// </summary>
        /// <param name="expansionMatrices">Матрицы распространения</param>
        /// <param name="allPoints">Все точки сети</param>
        /// <returns>Точка с минимальной суммой</returns>
        private static Point GetMinCostPoint(IDictionary<Point, ExpansionMatrixConteiner> expansionMatrices, IEnumerable<Point> allPoints)
        {
            
            var summCosts = new Dictionary<Point, double>();
            foreach (var matrixPoint in allPoints)
            {
                summCosts.Add(matrixPoint, 0d);
                foreach (var startPoint in expansionMatrices.Keys)
                {
                    summCosts[matrixPoint] += expansionMatrices[startPoint].ExpansionMatrix[matrixPoint];
                }
            }

            Point cps = null;
            var summCost = double.MaxValue;
            foreach (var matrixPoint in summCosts.Keys)
            {
                if (summCosts[matrixPoint] < summCost)
                {
                    cps = matrixPoint;
                    summCost = summCosts[matrixPoint];
                }
            }

            return cps;
        }

        /// <summary>
        /// Получение точки с минимальной стомостью прохода до (и от) целевой 
        /// </summary>
        /// <param name="expansionMatrices">Матрицы распространения</param>
        /// <param name="notTraversedStartPoints">Список непройденных точек. Среди них будет проводиться поиск оптимальной</param>
        /// <param name="collectionPoint">Целевая точка</param>
        /// <returns>Точка с минимальной стомостью прохода</returns>
        private static Point GetNearestPoint(
            IDictionary<Point, ExpansionMatrixConteiner> expansionMatrices,
            IEnumerable<Point> notTraversedStartPoints,
            Point collectionPoint)
        {
            Point nearestPoint = null;
            var minCost = double.MaxValue;

            foreach (var point in notTraversedStartPoints)
            {
                if (expansionMatrices[point].ExpansionMatrix[collectionPoint] < minCost)
                {
                    nearestPoint = point;
                    minCost = expansionMatrices[point].ExpansionMatrix[collectionPoint];
                }
            }

            return nearestPoint;
        }

       
        /// <summary>
        /// Поиск точки с минимальной эврестической функцией (F)
        /// </summary>
        /// <param name="points">Список точек</param>
        /// <returns>Точка с минимальной эврестической функцией</returns>
        private static Point GetPointWithMinF(HashSet<Point> points)
        {
            if (!points.Any())
            {
                throw new Exception("Пустой список точек");
            }
            var minF = double.MaxValue;
            Point resultPoint = null;
            foreach (var point in points)
            {
                if (point.F < minF)
                {
                    minF = point.F;
                    resultPoint = point;
                }
            }

            return resultPoint;
        }

        /// <summary>
        /// Восстановление оптимального пути
        /// </summary>
        /// <param name="goal">Целевая точка</param>
        /// <returns>Оптимальный путь до целевой точки</returns>
        private static IList<Point> ReconstructPath(Point goal)
        {
            var resultList = new List<Point>();

            var currentPoint = goal;

            while (currentPoint != null)
            {
                resultList.Add(currentPoint);
                currentPoint = currentPoint.CameFromPoint;
            }

            resultList.Reverse();

            return resultList;
        }

        private static IList<Point> ReconstructPath(Point goal, ExpansionMatrixConteiner expansionMatrixConteiner, IEnumerable<Point> allPoints)
        {
            var path = new List<Point>() {goal};
            var currentPoint = goal;
            while (expansionMatrixConteiner.ExpansionMatrix[currentPoint] > 0)
            {
                Point closestNeighbour = null;
                var minCost = double.MaxValue;
                foreach (var neihgbour in currentPoint.GetNeighbors(allPoints))
                {
                    if (expansionMatrixConteiner.ExpansionMatrix[neihgbour] < minCost)
                    {
                        minCost = expansionMatrixConteiner.ExpansionMatrix[neihgbour];
                        closestNeighbour = neihgbour;
                    }
                }
                currentPoint = closestNeighbour;
                path.Add(closestNeighbour);
            }

            return path;
        }
    }
}
