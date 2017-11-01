using System;
using System.Collections.Generic;
using Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk;
using Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk.Model;
using IPA.AStar;

namespace Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk.AStar
{
    
    public class Square : Point
    {

        private static double WOOD_WEIGHT = 0.5;

        public Dictionary<Square, double> AdditionalAngleCoeffs { get; set; }

        public Dictionary<Square, double> Angles { get; set; }

        private const double Eps = 1E-6;

        /// <summary>
        /// Длина стороны квадрата
        /// </summary>
        public double Side { get; set; }
        /// <summary>
        /// Координата x левого верхнего угла квадрата
        /// </summary>
        public double X { get; set; }
        /// <summary>
        /// Координата y левого верхнего улга квадрата
        /// </summary>
        public double Y { get; set; }
        /// <summary>
        /// "Вес" квадрата
        /// </summary>
        public double Weight { get; set; }
        /// <summary>
        /// Имя квадрата (для удобства идентификации)
        /// </summary>
        public string Name { get; set; }


        public IEnumerable<Square> Neighbors { get; set; }

        public HashSet<LivingUnit> Units { get; set; }

        private Game _game { get; set; }


        public Square(double side, double x, double y, double weight, string name, Game game)
        {
            Side = side;
            X = x;
            Y = y;
            Weight = weight;
            Name = name;
            AdditionalAngleCoeffs = new Dictionary<Square, double>();
            Angles = new Dictionary<Square, double>();
            _game = game;
            
        }

        public override IEnumerable<Point> GetNeighbors(IEnumerable<Point> points)
        {
            return Neighbors;
        }

        public override double GetHeuristicCost(Point goal)
        {
            //return GetManhattanDistance(this, (Square)goal);
            return GetEuclidDistance(this, (Square)goal);
            //if (AdditionalAngleCoeffs.ContainsKey(goal as Square))
            //{
            //    res += AdditionalAngleCoeffs[goal as Square];
            //}
            //return res;


        }

        private static double GetEuclidDistance(Square a, Square b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        private static double GetManhattanDistance(Square a, Square b)
        {
            var dx = Math.Abs(a.X - b.X);
            var dy = Math.Abs(a.Y - b.Y);
            return dx + dy;
        }

        public override double GetCost(Point goal, Wizard self, Game game)
        {           
            var dist = GetEuclidDistance(this, goal as Square);

            //var cost = (goal as Square).Weight + this.Weight;
            //cost += (goal as Square).AdditionalAngleCoeffs[this];

            var p0X = X + Side/2;
            var p0Y = Y + Side/2;
            var p1X = (goal as Square).X + Side / 2;
            var p1Y = (goal as Square).Y + Side / 2;

            var trees = new List<LivingUnit>();
            if (Units != null)
            {
                foreach (var unit in Units)
                {
                    var isCrossCircle = Intersect(p0X, p0Y, p1X, p1Y, unit.X, unit.Y, self.Radius, unit.Radius);
                    if (isCrossCircle)
                    {
                        if (unit is Tree)
                        {
                            if (!trees.Contains(unit)) trees.Add(unit);
                        }
                        else return 999999*dist;
                    }
                }
            }
            if ((goal as Square).Units != null)
            {
                foreach (var unit in (goal as Square).Units)
                {
                    var isCrossCircle = Intersect(p0X, p0Y, p1X, p1Y, unit.X, unit.Y, self.Radius, unit.Radius);

                    if (isCrossCircle)
                    {
                        if (unit is Tree)
                        {
                            if (!trees.Contains(unit)) trees.Add(unit);
                        }
                        else return 999999 * dist;
                    }
                }
            }

            var treesWeight = 0d;
            foreach (Tree tree in trees)
            {
                treesWeight += GetTreeWeight(tree); //TODO: * (1 + Math.Abs(self.GetAngleTo(tree)))
            }

            return ((goal as Square).Weight + treesWeight)*dist;           
        }

        private double GetTreeWeight(Tree tree)
        {
            var life = tree.Life;
            var staffCastCount = life/_game.StaffDamage + 1;
            return WOOD_WEIGHT*staffCastCount;
        }

        private static double DubDistance(double p0X, double p0Y, double p1X, double p1Y, double pX, double pY)
        {
            return ((p0Y - p1Y) * pX + (p1X - p0X) * pY + (p0X * p1Y - p1X * p0Y)) * ((p0Y - p1Y) * pX + (p1X - p0X) * pY + (p0X * p1Y - p1X * p0Y))
                   /
                   ((p1X - p0X) * (p1X - p0X) + (p1Y - p0Y) * (p1Y - p0Y));
        }

        private static double GetDubDistanceTo(double p0X, double p0Y, double p1X, double p1Y)
        {
            return (p0X - p1X)*(p0X - p1X) + (p0Y - p1Y)*(p0Y - p1Y);
        }

        public static bool Intersect(double p0X, double p0Y, double p1X, double p1Y, double pX, double pY, double r0, double r)
        {
            var minDist = (r0 + r) * (r0 + r);
            var dist = DubDistance(p0X, p0Y, p1X, p1Y, pX, pY);
            var lenLine = GetDubDistanceTo(p0X, p0Y, p1X, p1Y) + minDist; //гипотенуза
            var len0 = GetDubDistanceTo(pX, pY, p0X, p0Y);
            var len1 = GetDubDistanceTo(pX, pY, p1X, p1Y);
            return !(dist > minDist || len0 > lenLine && len1 > minDist || len1 > lenLine && len0 > minDist);
        }



        public override string ToString()
        {
            return "X: " + (X + Side / 2d) + "; Y: " + (Y + Side / 2d);
        } 
    }
}
