using Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk;
using Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk.AStar;
using IPA.AStar;

namespace Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk
{
    enum CloseBorder
    {
        None,
        Left,
        Top,
        Right,
        Bottom,
    }

    enum PointPosition
    {
        Left,
        Right,
        OnLine
    }



    public sealed class MyStrategy : IStrategy
    {


        //static MyStrategy()
        //{
        //    Debug.connect("localhost", 13579);
        //}

        private static double WAYPOINT_RADIUS = 100.0D;
        private static double ANEMY_WAYPOINT_RADIUS = 200.0D;

        private static double LOW_HP_FACTOR = 0.33D;
        private static double LOW_HP_BONUS_FACTOR = 0.5D;

        private static double BORDER_RADIUS = 100D;
        private static int ANEMY_BASE_SAFE_FRIEND_COUNT = 3;
        private static int DEFAULT_WEIGHT = 42;
        private static double ANEMY_BASE_SAFE_DISTANCE = 1000;
        private static double ROW_WIDTH = 400;
        private static double TOLERANCE = 1E-3;
        private static int BONUS_PATH_COUNT = 20;
        private static double STACK_FACTOR = 2d;
        private static double BONUS_ADD_TIME = 300;

        private static double BONUS_ADD_TIME_PER_SUARE = 1.1;

        private static double WOOD_WEIGHT = 2;
        private static int CHECK_MASTER_TIME = 200;

        private static double SHOOTING_SQUARE_WEIGHT = 2;

        /**
         * �������� ����� ��� ������ �����, ����������� ��������� ���������� ������������ ����������.
         * <p>
         * ���� �� ������, ��������� � ��������� ����� � ������� �����������.
         * ���� �������� ���� ��������� �������, ��������� � ���������� �����.
         */
        private IDictionary<LaneType, Point2D[]> _waypointsByLine = new Dictionary<LaneType, Point2D[]>();

        private Random _random;

        private LaneType _line;

        private Wizard _self;
        private World _world;
        private Game _game;
        private Move _move;

        private Square _startSquare;
        private Square _staticStartSquare;
        private Square[,] _table;
        private Square[,] _staticTable;
        private bool[,] _isVisibleSquares;
        private bool[,] _traversedSquares;
        private List<Square> _squares;
        private List<Square> _staticSquares;
        private double _squareSize;
        private double _staticSquareSize;
        private int _n;
        private int _staticN;
        private int _m;
        private int _staticM;
        private double _startX;
        private double _staticStartX;
        private double _startY;
        private double _staticStartY;

        private double _anemyBaseX;
        private double _anemyBaseY;

        private Building _selfBase;
        private double _criticalGoBackDistance;
        private double _criticalNearestTargetDistance;
        private bool _isAStarBuilt = false;
        private double _gotBonus0Time = 0d;
        private double _gotBonus1Time = 0d;

        private Point2D[] _bonusPoints = new Point2D[2] { new Point2D(1200d, 1200d), new Point2D(2800d, 2800d) };
        private Point2D _bonus0TopPoint;
        private Point2D _bonus0BottomPoint;
        private Point2D _bonus1TopPoint;
        private Point2D _bonus1BottomPoint;

        private HashSet<long> _seenTreesIds = new HashSet<long>();
        private IList<Building> _myBuildings;
        private IList<Building> _anemyBuildings;
        private IList<bool> _IsMyBuildingAlive;
        private IList<bool> _IsAnemyBuildingAlive;

        private IList<Point> _lastTickPath;
        private Point2D _lastTickResPoint;
        private Point2D _thisTickResPoint;

        private double _lastTickX;
        private double _lastTickY;

        private bool _needChangeLine;
        private bool _see0Bonus;
        private bool _see1Bonus;
        private bool _isBonus0OnMap;
        private bool _isBonus1OnMap;

        private HashSet<Tree> _trees = new HashSet<Tree>();

        private SkillType? _rangeSkillType = null;
        private SkillType? _magicalDamageSkillType = null;
        private SkillType? _staffDamageSkillType = null;
        private SkillType? _movementSkillType = null;
        private SkillType? _absorptionSkillType = null;

        private IDictionary<long, BulletStartData> _bulletStartDatas;

        private readonly SkillType[] _skillsOrder = new SkillType[]
        {

            SkillType.RangeBonusPassive1,
            SkillType.RangeBonusAura1,
            SkillType.RangeBonusPassive2,
            SkillType.RangeBonusAura2,
            SkillType.AdvancedMagicMissile,
            
            SkillType.StaffDamageBonusPassive1,
            SkillType.StaffDamageBonusAura1,
            SkillType.StaffDamageBonusPassive2,
            SkillType.StaffDamageBonusAura2,
            SkillType.Fireball,

            SkillType.MagicalDamageBonusPassive1,
            SkillType.MagicalDamageBonusAura1,
            SkillType.MagicalDamageBonusPassive2,
            SkillType.MagicalDamageBonusAura2,
            SkillType.FrostBolt,

            SkillType.MagicalDamageAbsorptionPassive1,
            SkillType.MagicalDamageAbsorptionAura1,
            SkillType.MagicalDamageAbsorptionPassive2,
            SkillType.MagicalDamageAbsorptionAura2,
            SkillType.Shield,

            SkillType.MovementBonusFactorPassive1,
            SkillType.MovementBonusFactorAura1,
            SkillType.MovementBonusFactorPassive2,
            SkillType.MovementBonusFactorAura2,
            SkillType.Haste,
            
        };


        public void Move(Wizard self, World world, Game game, Move move)
        {
            //Debug.beginPost();
            initializeTick(self, world, game, move);
            initializeStrategy(self, game);

            _move.SkillToLearn = GetSkillTypeToLearn();
            UpdateBulletStartDatas();
            SendMessage();

            if (_world.TickIndex == 301)
            {
                var emptyLane = GetEmptyLane();
                if (emptyLane != null) _line = emptyLane.Value;
            }
            //CheckMasterLine();
            //CheckNeedChangeLine();


            _see0Bonus =
                _world.Wizards.Any(w => w.Faction == _self.Faction && w.GetDistanceTo(_bonusPoints[0].X, _bonusPoints[0].Y) <= w.VisionRange);
            _see1Bonus =
                _world.Wizards.Any(w => w.Faction == _self.Faction && w.GetDistanceTo(_bonusPoints[1].X, _bonusPoints[1].Y) <= w.VisionRange);

            _isBonus0OnMap =
                _world.Bonuses.Any(
                    b => Math.Abs(b.X - _bonusPoints[0].X) < TOLERANCE && Math.Abs(b.Y - _bonusPoints[0].Y) < TOLERANCE);
            _isBonus1OnMap =
              _world.Bonuses.Any(
                  b => Math.Abs(b.X - _bonusPoints[1].X) < TOLERANCE && Math.Abs(b.Y - _bonusPoints[1].Y) < TOLERANCE);

            if (_see0Bonus && !_isBonus0OnMap && _gotBonus0Time > _game.BonusAppearanceIntervalTicks)
            {
                _gotBonus0Time = _world.TickIndex - _game.BonusAppearanceIntervalTicks * (_world.TickIndex / _game.BonusAppearanceIntervalTicks);
            }

            if (_see1Bonus && !_isBonus1OnMap && _gotBonus1Time > _game.BonusAppearanceIntervalTicks)
            {
                _gotBonus1Time = _world.TickIndex - _game.BonusAppearanceIntervalTicks * (_world.TickIndex / _game.BonusAppearanceIntervalTicks);
            }


            //if (_gotBonus0Time > _game.BonusAppearanceIntervalTicks && see0Bonus &&
            //    !_world.Bonuses.Any(b => Math.Abs(b.X - _bonusPoints[0].X) < TOLERANCE && Math.Abs(b.Y - _bonusPoints[0].Y) < TOLERANCE))
            //{
            //    _gotBonus0Time = 0;
            //}
            //if (_gotBonus1Time > _game.BonusAppearanceIntervalTicks && see1Bonus &&
            //    !_world.Bonuses.Any(b => Math.Abs(b.X - _bonusPoints[1].X) < TOLERANCE && Math.Abs(b.Y - _bonusPoints[1].Y) < TOLERANCE))
            //{
            //    _gotBonus1Time = 0;
            //}



            //� ������ ������, ������� ��������
            if ((_world.TickIndex - _gotBonus0Time) % _game.BonusAppearanceIntervalTicks > 0)
            {
                _gotBonus0Time += (_world.TickIndex - _gotBonus0Time) % _game.BonusAppearanceIntervalTicks;
            }
            if ((_world.TickIndex - _gotBonus1Time) % _game.BonusAppearanceIntervalTicks > 0)
            {
                _gotBonus1Time += (_world.TickIndex - _gotBonus1Time) % _game.BonusAppearanceIntervalTicks;
            }



            //if (!_isAStarBuilt)
            //{
            //    MakeStaticAStar();
            //    _isAStarBuilt = true;
            //}
            //UpdateStaticAStar();


            if (!_isAStarBuilt)
            {
                MakeDinamycAStar();
                //MakeStaticAStar();
                _isAStarBuilt = true;
            }
            UpdateDinamycAStar();
            UpdateLastTickPathTrees();


            //foreach (var tree in _trees)
            //{
            //    Debug.circle(tree.X, tree.Y, tree.Radius, 150);        
            //}

            //_staticStartSquare = _staticTable[GetStaticSquareI(_self.X), GetStaticSquareJ(_self.Y)];



            //for (int i = 0; i < _n; ++i)
            //{
            //    for (int j = 0; j < _m; ++j)
            //    {
            //        var dist = _self.GetDistanceTo(_table[i, j].X, _table[i, j].Y);
            //        if (dist > _self.CastRange) continue;

            //        if (_table[i, j].Weight == SHOOTING_SQUARE_WEIGHT)
            //        {
            //            Debug.rect(_table[i, j].X, _table[i, j].Y, _table[i, j].X + _table[i, j].Side,
            //                _table[i, j].Y + _table[i, j].Side, 150);
            //        }
            //    }
            //}



            _criticalGoBackDistance = _self.CastRange * 1.5;
            _selfBase =
                      _world.Buildings.Single(x => x.Faction == _self.Faction && x.Type == BuildingType.FactionBase);

            _anemyBaseX = _world.Width - _selfBase.X;
            _anemyBaseY = _world.Height - _selfBase.Y;

            var nearestStaffTarget = GetNearestStaffRangeTarget();
            var shootingTarget = GetShootingTarget();

            var goBonusResult = CheckAndGoForBonus(nearestStaffTarget, shootingTarget);
            //var goBonusResult = new GoBonusResult()
            //{
            //    IsGo = false,
            //    IsWoodCut = false
            //};

            var runBackTime = 0d;
            
            var canGoOnStaffRange = CanGoToStaffRangeNew(ref runBackTime);

            if (nearestStaffTarget != null)
            {
                double angle = self.GetAngleTo(nearestStaffTarget);
                move.Turn = angle;

                if (Math.Abs(angle) <= _game.StaffSector / 2.0D)
                {
                    InitializeShootingAction(nearestStaffTarget, true);
                }

                var bullet = GetBulletFlyingInMe();
                if (!goBonusResult.IsGo && (!canGoOnStaffRange || bullet != null || NeedGoBack())) // && NeedGoBack(CanGoOnStaffRange(nearestStaffTarget))
                {
                    MakeGoBack(bullet, runBackTime);
                }

            }
            else
            {

                var closestTarget = GetClosestTarget();
                //var canGoOnStaffRange = CanGoOnStaffRange(closestTarget);

                Point2D radiusPoint = null;
               
                if (shootingTarget != null)
                {

                    double angle = self.GetAngleTo(shootingTarget);
                    if (!goBonusResult.IsWoodCut)
                    {
                        move.Turn = angle;
                    }

                    if (!goBonusResult.IsGo)
                    {
                        if (canGoOnStaffRange)
                        {
                            _thisTickResPoint = new Point2D(closestTarget.X, closestTarget.Y);
                            goTo(new Point2D(closestTarget.X, closestTarget.Y), _game.StaffRange + closestTarget.Radius - TOLERANCE,
                                _game.StaffRange + closestTarget.Radius - TOLERANCE, false);
                        }

                        else
                        {
                            var radius = _self.CastRange - shootingTarget.Radius +
                                         _game.MagicMissileRadius * 1.5;

                            radiusPoint = GetRadiusPoint(ROW_WIDTH * 0.75, radius, shootingTarget);
                            if (radiusPoint != null)
                            {
                                _thisTickResPoint = radiusPoint;
                                //Debug.circle(radiusPoint.X, radiusPoint.Y, _self.Radius, 200);
                                goTo(radiusPoint, _self.Radius * 2, 0d, false);
                            }
                        }

                    }


                    if (CanShoot(shootingTarget))
                    {
                        InitializeShootingAction(shootingTarget, false);
                    }
                    //else
                    //{
                    //    if (!isGoForBonus)
                    //    {
                    //        move.StrafeSpeed = 0d;
                    //        move.Speed = 0d;
                    //    }
                    //}
                }
                else
                {
                    if (!goBonusResult.IsGo)
                    {
                        if (_needChangeLine)
                        {
                            var doNotTurn = closestTarget != null &&
                                            _self.GetDistanceTo(closestTarget) <= _self.CastRange * 1.5;
                            var basePoint = new Point2D(100.0D, _world.Height - 100.0D);
                            _thisTickResPoint = basePoint;
                            goTo(basePoint, _self.CastRange, 0d, !doNotTurn);
                        }

                        //if (!IsOnLine(_self))
                        //{
                        //    var closestTarget = GetClosestTarget();
                        //    var doNotTurn = closestTarget != null &&
                        //                    _self.GetDistanceTo(closestTarget) <= _self.CastRange*1.5;
                        //    goTo(new Point2D(100.0D, _world.Height - 100.0D), _self.CastRange, false, !doNotTurn);

                        //}
                        else
                        {
                            var nearestBaseTarget = GetNearestMyBaseAnemy();
                            //                            var isAnemyBase = nearestBaseTarget is Building && (nearestBaseTarget as Building).Type == BuildingType.FactionBase;

                            if (nearestBaseTarget != null)
                            {
                                var needTurn = true;
                                if (closestTarget != null && _self.GetDistanceTo(closestTarget) < _self.CastRange * 1.5)
                                {
                                    move.Turn = _self.GetAngleTo(closestTarget);
                                    needTurn = false;
                                }

                                var radius = _self.CastRange - nearestBaseTarget.Radius +
                                             _game.MagicMissileRadius * 1.5;
                                radiusPoint = GetRadiusPoint(ROW_WIDTH * 0.75, radius, nearestBaseTarget);

                                _thisTickResPoint = new Point2D(nearestBaseTarget.X, nearestBaseTarget.Y);
                                if (radiusPoint != null)
                                {
                                    //Debug.circle(radiusPoint.X, radiusPoint.Y, _self.Radius, 200);
                                    goTo(radiusPoint, _self.Radius * 2, _self.Radius * 2, needTurn);
                                    _lastTickResPoint = new Point2D(nearestBaseTarget.X, nearestBaseTarget.Y);
                                    //��������� ������� �����, � �� �������� ����� ����
                                }
                                else
                                {
                                    goTo(
                                        new Point2D(nearestBaseTarget.X, nearestBaseTarget.Y),
                                        _self.Radius + nearestBaseTarget.Radius + 10000 * TOLERANCE,
                                        _self.Radius + nearestBaseTarget.Radius + 10000 * TOLERANCE,
                                        needTurn);
                                }

                            }
                            else
                            {
                                //�� ����� ��� �����
                                Building nearTower = null; // башня врага, которую не видим
                                for (int i = 0; i < _anemyBuildings.Count; ++i)
                                {
                                    if (!_IsAnemyBuildingAlive[i]) continue;
                                    if (!IsStrongOnLine(_anemyBuildings[i], _line)) continue;
                                    if (_anemyBuildings[i].GetDistanceTo(_self) <
                                        _anemyBuildings[i].AttackRange + 2 * _self.Radius)
                                    {
                                        nearTower = _anemyBuildings[i];
                                        break;
                                    }
                                }

                                if (nearTower == null)
                                {
                                    var nextWaypoint = getNextWaypoint();
                                    _thisTickResPoint = nextWaypoint;
                                    goTo(nextWaypoint, _self.Radius * 4, 0d, true);
                                }
                                else
                                {
                                    _move.Turn = _self.GetAngleTo(nearTower);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!goBonusResult.IsWoodCut && closestTarget != null && _self.GetDistanceTo(closestTarget) < 1.5 * _self.CastRange)
                        {
                            _move.Turn = _self.GetAngleTo(closestTarget);
                        }
                    }
                }

                var bullet = GetBulletFlyingInMe();
                if (!goBonusResult.IsGo && (!canGoOnStaffRange || bullet != null || NeedGoBack()))
                {
                    MakeGoBack(bullet, runBackTime);
                }
            }

            _lastTickX = _self.X;
            _lastTickY = _self.Y;

            _gotBonus0Time++;
            _gotBonus1Time++;

            //if (isGoForBonus) return;

            //var nearestBaseTarget = GetNearestMyBaseAnemy();
            //if (nearestBaseTarget != null)
            //{
            //    goTo(
            //        new Point2D(nearestBaseTarget.X, nearestBaseTarget.Y),
            //        _self.CastRange - _self.Radius,
            //        false,
            //        _self.GetDistanceTo(nearestBaseTarget) > _self.CastRange * 1.5);
            //    return;
            //}








            //���� ����� ���������� ...
            //if (nearestTarget != null)
            //{
            //    // ... �� �������������� � ����.
            //    double angle = self.GetAngleTo(nearestTarget);
            //    move.Turn = angle;
            //    double distance = self.GetDistanceTo(nearestTarget);

            //    if (distance <= _self.CastRange && Math.Abs(angle) < game.StaffSector/2.0D)
            //    {
            //        // ... �� �������.

            //        move.Speed = 0;

            //        if (self.RemainingActionCooldownTicks == 0 &&
            //            self.RemainingCooldownTicksByAction[(int) ActionType.MagicMissile] == 0)
            //        {
            //            move.Action = ActionType.MagicMissile;
            //            move.CastAngle = angle;
            //            move.MinCastDistance = distance - nearestTarget.Radius + game.MagicMissileRadius;
            //        }
            //    }

            //    var isGoForBonus = CheckAndGoForBonus(distance);
            //    if (isGoForBonus) return;


            //    if (distance > _self.CastRange)
            //    {
            //        var nearestBaseTarget = GetNearestMyBaseAnemy();
            //        goTo(
            //            new Point2D(nearestBaseTarget.X, nearestBaseTarget.Y),
            //            _self.CastRange - _self.Radius,
            //            false,
            //            distance > _self.CastRange*1.5);
            //    }

            //    if (NeedGoBack())
            //    {
            //        var closestTarget = GetClosestTarget();
            //        if (closestTarget != null) _move.Turn = _self.GetAngleTo(closestTarget);
            //        GoBack();
            //    }
            //    return;
            //}
            //else
            //{
            //    var isGoForBonus = CheckAndGoForBonus(double.MaxValue);
            //    if (isGoForBonus) return;

            //    if (NeedGoBack())
            //    {
            //        var closestTarget = GetClosestTarget();
            //        if (closestTarget != null) _move.Turn = _self.GetAngleTo(closestTarget);
            //        GoBack();
            //        return;
            //    }
            //}


            //goTo(nextWaypoint, _self.Radius * 4, false, true);

            //Debug.endPost();
        }

        private void MakeGoBack(BulletStartData bullet, double runBackTime)
        {
            if (bullet != null)
            {
                var time = GetBulletTime(bullet, _self);

                var canGoBack = CanGoBack(_self, bullet, time, false);
                var canGoLeft = CanGoLeft(_self, bullet, time);
                var canGoRight = CanGoRight(_self, bullet, time);
                if (canGoBack)
                {
                    _move.Speed = -GetWizardMaxBackSpeed(_self);
                    _move.StrafeSpeed = 0;
                }
                else if (canGoLeft)
                {
                    _move.Speed = 0;
                    _move.StrafeSpeed = -GetWizardMaxStrafeSpeed(_self);
                    _move.Turn = 0;
                }
                else if (canGoRight)
                {
                    _move.Speed = 0;
                    _move.StrafeSpeed = GetWizardMaxStrafeSpeed(_self);
                    _move.Turn = 0;
                }
                else
                {
                    GoBack();
                }
            }
            else
            {

                //                if (CanGoBackStraightPoint(_self, runBackTime))
                //                {
                //                    _move.Speed = -GetWizardMaxBackSpeed(_self);
                //                    _move.StrafeSpeed = 0;
                //                }
                //                else
                //                {
                GoBack();
                //}
            }
        }

        private void InitializeShootingAction(LivingUnit shootingTarget, bool canStaff)
        {
            double angle = _self.GetAngleTo(shootingTarget);
            double distance = _self.GetDistanceTo(shootingTarget);
            var shootingWizard = shootingTarget as Wizard;
            var shootingBuilding = shootingTarget as Building;

            var canFireballWizard = shootingWizard != null && CanShootWithFireball(_self, _self.X, _self.Y, shootingWizard, 0);
            var canFireballBuilding = shootingBuilding != null;
            //_self.GetDistanceTo(shootingBuilding) - _self.Radius > _game.FireballExplosionMinDamageRange;


            var nearAnemies = new List<LivingUnit>();
            nearAnemies.AddRange(
                _world.Wizards.Where(
                    x =>
                        shootingTarget.Faction == x.Faction &&
                        shootingTarget.GetDistanceTo(x) - x.Radius <= _game.FireballExplosionMinDamageRange));
            nearAnemies.AddRange(
               _world.Minions.Where(
                   x =>
                       shootingTarget.Faction == x.Faction &&
                       shootingTarget.GetDistanceTo(x) - x.Radius <= _game.FireballExplosionMinDamageRange));
            nearAnemies.AddRange(
              _world.Buildings.Where(
                  x =>
                      shootingTarget.Faction == x.Faction &&
                      shootingTarget.GetDistanceTo(x) - x.Radius <= _game.FireballExplosionMinDamageRange));
            var canFiballUnit = nearAnemies.Count >= 3;

            if (canStaff && _self.RemainingActionCooldownTicks == 0 &&
                _self.RemainingCooldownTicksByAction[(int)ActionType.Staff] == 0)
            {
                _move.Action = ActionType.Staff;
            }
            else if (_self.RemainingActionCooldownTicks == 0 && _self.Skills.Any(x => x == SkillType.Fireball) &&
                     (canFireballWizard || canFireballBuilding || canFiballUnit) &&
                     (_self.GetDistanceTo(shootingTarget) - _self.Radius > _game.FireballExplosionMinDamageRange) &&
                     _self.RemainingCooldownTicksByAction[(int)ActionType.Fireball] == 0)
            {
                _move.Action = ActionType.Fireball;
                _move.CastAngle = angle;
                _move.MinCastDistance = distance - shootingTarget.Radius + _game.FireballRadius;
            }
            else if (_self.RemainingActionCooldownTicks == 0 && _self.Skills.Any(x => x == SkillType.FrostBolt) &&
                     shootingWizard != null && CanShootWithFrostBolt(_self, _self.X, _self.Y, shootingWizard, 0) &&
                     _self.RemainingCooldownTicksByAction[(int)ActionType.FrostBolt] == 0)
            {
                _move.Action = ActionType.FrostBolt;
                _move.CastAngle = angle;
                _move.MinCastDistance = distance - shootingTarget.Radius + _game.FrostBoltRadius;
            }
            else if (_self.RemainingActionCooldownTicks == 0 &&
                     _self.RemainingCooldownTicksByAction[(int)ActionType.MagicMissile] == 0)
            {
                _move.Action = ActionType.MagicMissile;
                _move.CastAngle = angle;
                _move.MinCastDistance = distance - shootingTarget.Radius + _game.MagicMissileRadius;
            }
        }

        private IEnumerable<LivingUnit> GetBeforeFriends(LivingUnit target)
        {
            var friends = new List<LivingUnit>();
            friends.AddRange(_world.Wizards.Where(x => x.Faction == _self.Faction && !x.IsMe));
            friends.AddRange(_world.Minions.Where(x => x.Faction == _self.Faction));
            var selfDist = _self.GetDistanceTo(target);
            return friends.Where(f => f.GetDistanceTo(target) <= selfDist);
        }

        private bool IsOnBase()
        {
            return _self.X <= 2 * ROW_WIDTH && _self.Y >= _world.Height - 2 * ROW_WIDTH;
        }

        private Point2D GetStraightRelaxPoint(double x, double y, double relaxCoeff)
        {
            var length = _self.GetDistanceTo(x, y) - relaxCoeff;
            var coeff = length / _self.GetDistanceTo(x, y);
            var relaxX = _self.X + (x - _self.X) * coeff;
            var relaxY = _self.Y + (y - _self.Y) * coeff;
            return new Point2D(relaxX, relaxY);
        }

        private Point2D GetGoStraightPoint(double x, double y, double relaxCoeff)
        {
            var relaxP = GetStraightRelaxPoint(x, y, relaxCoeff);

            if (_self.GetDistanceTo(relaxP.X, relaxP.Y) < TOLERANCE) return new Point2D(relaxP.X, relaxP.Y);

            var units = new List<LivingUnit>();
            units.AddRange(_world.Wizards.Where(w => !w.IsMe));
            units.AddRange(_world.Minions);
            units.AddRange(_world.Buildings);
            units.AddRange(_trees);

            foreach (var unit in units)
            {
                if (Square.Intersect(_self.X, _self.Y, relaxP.X, relaxP.Y, unit.X, unit.Y, _self.Radius, unit.Radius)) return null;
            }

            return new Point2D(relaxP.X, relaxP.Y);
        }



        private bool CanGoBackStraightPoint(Wizard target, double time)
        {
            var newX = target.X - GetWizardMaxBackSpeed(target) * time * Math.Cos(target.Angle);
            var newY = target.Y - GetWizardMaxBackSpeed(target) * time * Math.Sin(target.Angle);

            if (newX < target.Radius || newY < target.Radius || newX > _world.Width - target.Radius ||
                newY > _world.Height - target.Radius)
                return false;

            return GetGoStraightPoint(newX, newY, 0) != null;
        }


        /// <summary>
        /// Может ли волшебник отойти от пули bsd. time - время на отход
        /// </summary>
        /// <param name="target"></param>
        /// <param name="bsd"></param>
        /// <param name="time"></param>
        /// <param name="ignoreGoStraightPoint"></param>
        /// <returns></returns>
        private bool CanGoBack(Wizard target, BulletStartData bsd, double time, bool ignoreGoStraightPoint)
        {
            var newX = target.X - GetWizardMaxBackSpeed(target) * time * Math.Cos(target.Angle);
            var newY = target.Y - GetWizardMaxBackSpeed(target) * time * Math.Sin(target.Angle);

            //var canGo = CanGoSide(bsd, myX, myY);

            var isIntersect = Square.Intersect(
                bsd.StartX,
                bsd.StartY,
                bsd.EndX,
                bsd.EndY,
                newX,
                newY,
                bsd.Radius,
                target.Radius);


            var canGoStraightPoint = true;
            if (!ignoreGoStraightPoint) canGoStraightPoint = CanGoBackStraightPoint(target, time);


            return !isIntersect && canGoStraightPoint;
        }

        private bool CanGoLeft(Wizard target, BulletStartData bsd, double time)
        {

            var newX = target.X + GetWizardMaxStrafeSpeed(target) * time * Math.Cos(target.Angle - Math.PI / 2);
            var newY = target.Y + GetWizardMaxStrafeSpeed(target) * time * Math.Sin(target.Angle - Math.PI / 2);

            if (newX < target.Radius || newY < target.Radius || newX > _world.Width - target.Radius ||
                newY > _world.Height - target.Radius)
                return false;

            //var canGo = CanGoSide(bsd, myX, myY);

            var isIntersect = Square.Intersect(
                bsd.StartX,
                bsd.StartY,
                bsd.EndX,
                bsd.EndY,
                newX,
                newY,
                bsd.Radius,
                target.Radius);

            return !isIntersect && GetGoStraightPoint(newX, newY, 0) != null;
        }

        private bool CanGoRight(Wizard target, BulletStartData bsd, double time)
        {

            var newX = target.X + GetWizardMaxStrafeSpeed(target) * time * Math.Cos(target.Angle + Math.PI / 2);
            var newY = target.Y + GetWizardMaxStrafeSpeed(target) * time * Math.Sin(target.Angle + Math.PI / 2);

            if (newX < target.Radius || newY < target.Radius || newX > _world.Width - target.Radius ||
                newY > _world.Height - target.Radius)
                return false;

            //var canGo = CanGoSide(bsd, myX, myY);

            var isIntersect = Square.Intersect(
                bsd.StartX,
                bsd.StartY,
                bsd.EndX,
                bsd.EndY,
                newX,
                newY,
                bsd.Radius,
                target.Radius);

            return !isIntersect && GetGoStraightPoint(newX, newY, 0) != null;
        }


        private LivingUnit GetNearestMyBaseAnemy()
        {
            var units = new List<LivingUnit>();
            units.AddRange(_world.Buildings);
            units.AddRange(_world.Wizards);
            units.AddRange(_world.Minions);
            var minDist = double.MaxValue;
            LivingUnit nearestUnit = null;
            foreach (var unit in units)
            {
                if (unit.Faction == _self.Faction)
                {
                    continue;
                }

                if (IsCalmNeutralMinion(unit)) continue;

                if (!IsStrongOnLine(unit, _line)) continue;
                var dist = _selfBase.GetDistanceTo(unit);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearestUnit = unit;
                }
            }
            return nearestUnit;
        }


        private double GetPathTime(IList<Point> path, Point2D resPoint)
        {
            var goStraightPoint = GetGoStraightPoint(resPoint.X, resPoint.Y, 0d);
            if (goStraightPoint != null)
            {
                var dist = _self.GetDistanceTo(goStraightPoint.X, goStraightPoint.Y);
                return dist / GetWizardMaxForwardSpeed(_self);
            }


            var fullTime = 0d;
            for (int i = 0; i < path.Count - 1; ++i)
            {
                var dist = path[i].GetHeuristicCost(path[i + 1]);
                var time = dist / GetWizardMaxForwardSpeed(_self) * BONUS_ADD_TIME_PER_SUARE; //TODO: �������� �� ���� ����
                fullTime += time;
            }
            return fullTime;
        }

        private GoBonusResult CheckAndGoForBonus(LivingUnit nearestStaffRangeTarget, LivingUnit shootingTarget)
        {
            var goBonusResult = new GoBonusResult()
            {
                IsGo = false,
                IsWoodCut = false
            };

            //не идем, если атакуем дохлую башню
            var nearestStaffRangeTargetBuilding = nearestStaffRangeTarget as Building;
            var shootingTargetBuilding = shootingTarget as Building;
            if (nearestStaffRangeTargetBuilding != null &&
                nearestStaffRangeTargetBuilding.Life <= nearestStaffRangeTargetBuilding.MaxLife * 0.25 ||
                shootingTargetBuilding != null && shootingTargetBuilding.Life <= shootingTargetBuilding.MaxLife * 0.25)
            {
                return goBonusResult;
            }

            var eps = _self.Radius * 2;
            var dangerousAnemies = GetDangerousAnemies(_self, eps).Where(x => x is Wizard);
            var friends = new List<LivingUnit>();
            foreach (var anemy in dangerousAnemies)
            {
                var currentFriends = GetDangerousAnemies(anemy, eps * 2).Where(x => x is Wizard && !friends.Contains(x));
                friends.AddRange(currentFriends);
            }

            if (dangerousAnemies.Count() > friends.Count ||
                _self.Life < _self.MaxLife * LOW_HP_FACTOR && IsInDangerousArea(_self, eps))
            {
                return goBonusResult;
            }

            //if (_self.Life < _self.MaxLife * LOW_HP_BONUS_FACTOR) return goBonusResult;
            var maxLength = 20 * _self.Radius * 2;
            var pathMaxLength = maxLength / _squareSize;
            var closestTarget = GetClosestTarget();
            var needTurn = closestTarget == null || _self.GetDistanceTo(closestTarget) > _self.CastRange + _self.Radius * 5;


            IList<Point> path0 = null;
            var length0 = _self.GetDistanceTo(_bonusPoints[0].X, _bonusPoints[0].Y);
            double path0Weight = double.MaxValue, path1Weight = double.MaxValue;

            double dt0 = double.MaxValue, dt1 = double.MaxValue;
            double path0Time = double.MaxValue, path1Time = double.MaxValue;

            var relaxCoeff = _self.Radius + _game.BonusRadius + TOLERANCE * 10;
            double relax0Coeff = 0, relax1Coeff = 0;

            if (length0 <= maxLength && _world.TickIndex < _world.TickCount - 1000)
            {
                var i0 = GetSquareI(_bonusPoints[0].X);
                var j0 = GetSquareJ(_bonusPoints[0].Y);
                relax0Coeff = _isBonus0OnMap ? 0d : relaxCoeff;
                path0 = Calculator.GetPath(_startSquare, _table[i0, j0], _squares, _self, _game, relaxCoeff);
                _thisTickResPoint = _bonusPoints[0];
                path0 = GetCorrectPath(path0);
                path0Time = GetPathTime(path0, _bonusPoints[0]);
                path0Weight = GetPathWeight(path0);
                dt0 = _game.BonusAppearanceIntervalTicks - (_gotBonus0Time + path0Time + BONUS_ADD_TIME);
                if (path0.Count > pathMaxLength || dt0 > 0) path0 = null;

            }


            IList<Point> path1 = null;
            var length1 = _self.GetDistanceTo(_bonusPoints[1].X, _bonusPoints[1].Y);
            if (length1 <= maxLength && _world.TickIndex < _world.TickCount - 1000)
            {
                var i1 = GetSquareI(_bonusPoints[1].X);
                var j1 = GetSquareJ(_bonusPoints[1].Y);
                relax1Coeff = _isBonus1OnMap ? 0d : relaxCoeff;
                path1 = Calculator.GetPath(_startSquare, _table[i1, j1], _squares, _self, _game, relaxCoeff);
                _thisTickResPoint = _bonusPoints[1];
                path1 = GetCorrectPath(path1);
                path1Time = GetPathTime(path1, _bonusPoints[1]);
                path1Weight = GetPathWeight(path1);
                dt1 = _game.BonusAppearanceIntervalTicks - (_gotBonus1Time + path1Time + BONUS_ADD_TIME);
                if (path1.Count > pathMaxLength || dt1 > 0) path1 = null;

            }

            bool isWoodCut = false;
            if (path0 != null && path1 != null)
            {
                if (path0Weight < path1Weight)
                {
                    GoToBonus(_bonusPoints[0], relaxCoeff, relax0Coeff, needTurn, path0, _gotBonus0Time, ref isWoodCut);
                }
                else
                {
                    GoToBonus(_bonusPoints[1], relaxCoeff, relax1Coeff, needTurn, path1, _gotBonus1Time, ref isWoodCut);
                }
                goBonusResult.IsGo = true;
                goBonusResult.IsWoodCut = isWoodCut;
            }
            else if (path0 != null && dt1 > path0Time)
            {
                GoToBonus(_bonusPoints[0], relaxCoeff, relax0Coeff, needTurn, path0, _gotBonus0Time, ref isWoodCut);

                goBonusResult.IsGo = true;
                goBonusResult.IsWoodCut = isWoodCut;

            }
            else if (path1 != null && dt0 > path1Time)
            {
                GoToBonus(_bonusPoints[1], relaxCoeff, relax1Coeff, needTurn, path1, _gotBonus1Time, ref isWoodCut);

                goBonusResult.IsGo = true;
                goBonusResult.IsWoodCut = isWoodCut;
            }
            return goBonusResult;

        }


        private void GoToBonus(Point2D bonusPoint, double relaxCoeff, double realRelaxCoeff, bool needTurn,
            IList<Point> path, double gotBonusTime, ref bool isWoodCut)
        {

            var goStraightPoint = GetGoStraightPoint(bonusPoint.X, bonusPoint.Y, relaxCoeff - TOLERANCE * 10000);
            if (goStraightPoint != null)
            {

                var nearstWizard = GetClosestToBonusWizard(bonusPoint);
                Point2D wizardPreventPoint = null;
                if (nearstWizard != null)
                {
                    var crossPoints = GetCrossPoints(
                        bonusPoint.X,
                        bonusPoint.Y,
                        relaxCoeff,
                        bonusPoint.X,
                        bonusPoint.Y,
                        nearstWizard.X,
                        nearstWizard.Y);

                    var wizardCp = crossPoints.OrderBy(x => x.getDistanceTo(nearstWizard)).FirstOrDefault();
                    if (wizardCp != null)
                    {
                        //                    var myCp = crossPoints.OrderBy(x => x.getDistanceTo(_self)).FirstOrDefault();
                        var strPoint = GetGoStraightPoint(wizardCp.X, wizardCp.Y, 0);
                        if (strPoint != null)
                        {
                            var selfTime = _self.GetDistanceTo(wizardCp.X, wizardCp.Y) / GetWizardMaxForwardSpeed(_self);
                            var nearstWizardTime = nearstWizard.GetDistanceTo(wizardCp.X, wizardCp.Y) /
                                                   GetWizardMaxForwardSpeed(nearstWizard);
                            var delta = 2;

                            if (selfTime < nearstWizardTime - delta &&
                                selfTime < (_game.BonusAppearanceIntervalTicks - _gotBonus0Time) - delta)
                            {
                                wizardPreventPoint = GetGoStraightPoint(wizardCp.X, wizardCp.Y, 0);
                            }

                        }

                    }
                }

                if (realRelaxCoeff == 0)
                {
                    _thisTickResPoint = new Point2D(bonusPoint.X, bonusPoint.Y);
                    isWoodCut = goTo(
                        bonusPoint,
                        relaxCoeff - TOLERANCE * 100,
                        relaxCoeff - TOLERANCE * 100,
                        needTurn,
                        path);
                }
                else if (wizardPreventPoint != null)
                {
                    _thisTickResPoint = new Point2D(wizardPreventPoint.X, wizardPreventPoint.Y);
                    goTo(wizardPreventPoint, 0, 0, false);
                    if (needTurn)
                    {
                        _move.Turn = _self.GetAngleTo(bonusPoint.X, bonusPoint.Y);
                    }
                }
                else
                {
                    _thisTickResPoint = new Point2D(bonusPoint.X, bonusPoint.Y);
                    isWoodCut = goTo(
                        bonusPoint,
                        relaxCoeff,
                        relaxCoeff,
                        needTurn,
                        path);
                }
            }
            else
            {
                if (realRelaxCoeff == 0 || bonusPoint.getDistanceTo(_self) > _self.CastRange / 2)
                {
                    _thisTickResPoint = new Point2D(bonusPoint.X, bonusPoint.Y);
                    isWoodCut = goTo(bonusPoint, relaxCoeff - TOLERANCE * 100, relaxCoeff - TOLERANCE * 100, needTurn,
                        path);
                }
                else
                {
                    isWoodCut = MakeBonusPath(bonusPoint, needTurn);
                }
            }
        }

        private void UpdateBulletStartDatas()
        {
            var anemyBullets = _world.Projectiles.Where(x => x.Faction != _self.Faction).ToList();
            var itemsToRemove = _bulletStartDatas.Where(bd => anemyBullets.All(b => b.Id != bd.Key)).ToArray();
            foreach (var item in itemsToRemove) _bulletStartDatas.Remove(item.Key);

            foreach (var bullet in anemyBullets)
            {
                if (!_bulletStartDatas.ContainsKey(bullet.Id))
                {
                    var source = _world.Wizards.SingleOrDefault(w => bullet.OwnerUnitId == w.Id);
                    double speed = 0;
                    if (bullet.Type == ProjectileType.Dart)
                    {
                        speed = _game.DartSpeed;
                    }
                    else if (bullet.Type == ProjectileType.MagicMissile)
                    {
                        speed = _game.MagicMissileSpeed;
                    }
                    else if (bullet.Type == ProjectileType.Fireball)
                    {
                        speed = _game.FireballSpeed;
                    }
                    else if (bullet.Type == ProjectileType.FrostBolt)
                    {
                        speed = _game.FrostBoltSpeed;
                    }

                    _bulletStartDatas.Add(
                        bullet.Id,
                        new BulletStartData(
                            source != null ? source.X : bullet.X,
                            source != null ? source.Y : bullet.Y,
                            source != null ? source.CastRange : _self.CastRange,
                            bullet.SpeedX,
                            bullet.SpeedY,
                            bullet.Radius,
                            speed,
                            bullet));

                }
                else
                {
                    _bulletStartDatas[bullet.Id].Bullet = bullet;
                }
            }
        }

        private bool CanShootWizard(Wizard source, Wizard target)
        {
            var angle = source.GetAngleTo(target);
            var deltaAngle = Math.Abs(angle) - _game.StaffSector / 2;
            double turnTime;
            if (deltaAngle <= 0)
            {
                turnTime = 0;
            }
            else
            {
                turnTime = (int)(deltaAngle / GetWizardMaxTurn(source)) + 1;
            }

            var startX = source.X;
            var startY = source.Y;

            return CanShootWithMissle(source, startX, startY, target, turnTime);
        }

        private bool NeedRunBack(LivingUnit source, Wizard target, IList<LivingUnit> friends, bool isNextStep)
        {
            var dist = source.GetDistanceTo(target) - target.Radius;
            var minion = source as Minion;
            //if (minion != null && minion.Type == MinionType.FetishBlowdart)
            //{
            //    var angle = minion.GetAngleTo(_self);
            //    var deltaAngle = Math.Abs(angle) - _game.FetishBlowdartAttackSector / 2;
            //    double turnTime;
            //    if (deltaAngle <= 0)
            //    {
            //        turnTime = 0;
            //    }
            //    else
            //    {
            //        turnTime = (int)(deltaAngle / _game.MinionMaxTurnAngle) + 1;
            //    }

            //    var bsd = new BulletStartData(
            //        minion.X,
            //        minion.Y,
            //        _game.FetishBlowdartAttackRange,
            //        _self.X - minion.X,
            //        _self.Y - minion.Y,
            //        _self.Radius,
            //        _game.DartSpeed);

            //    var nextTickTime = dist / bsd.Speed + minion.RemainingActionCooldownTicks + turnTime - 2;

            //    var canGoBack = CanGoBack(bsd, nextTickTime);
            //    var canGoLeft = CanGoLeft(bsd, nextTickTime);
            //    var canGoRight = CanGoRight(bsd, nextTickTime);
            //    if (!canGoBack && !canGoLeft && !canGoRight) return true;
            //    return false;

            //}

            if (minion != null)
            {
                var orderedFriends = friends.OrderBy(x => x.GetDistanceTo(source));
                var firstFriend = orderedFriends.First() as Wizard;
                if (firstFriend != null && firstFriend.IsMe) return true;
            }

            var wizard = source as Wizard;
            if (wizard != null)
            {
                var angle = wizard.GetAngleTo(target);
                var deltaAngle = Math.Abs(angle) - _game.StaffSector / 2;
                double turnTime;
                if (deltaAngle <= 0)
                {
                    turnTime = 0;
                }
                else
                {
                    turnTime = (int)(deltaAngle / GetWizardMaxTurn(wizard)) + 1;
                }

                double startX, startY;
                Wizard newTarget;
                if (!isNextStep)
                {
                    startX = wizard.X;
                    startY = wizard.Y;
                    newTarget = target;
                }
                else
                {
                    var cooldownTime = wizard.RemainingCooldownTicksByAction[(int)ActionType.MagicMissile];
                    var wizardTagetAngle = wizard.Angle + wizard.GetAngleTo(target);
                    var targetWizardAngle = target.Angle + target.GetAngleTo(wizard);

                    startX = wizard.X + GetWizardMaxForwardSpeed(wizard) * Math.Cos(wizardTagetAngle) * (cooldownTime + 2);
                    startY = wizard.Y + GetWizardMaxForwardSpeed(wizard) * Math.Sin(wizardTagetAngle) * (cooldownTime + 2);

                    var targetX = target.X - GetWizardMaxBackSpeed(target) * Math.Cos(targetWizardAngle) * (cooldownTime - 2);
                    var targetY = target.Y - GetWizardMaxBackSpeed(target) * Math.Sin(targetWizardAngle) * (cooldownTime - 2);

                    newTarget = new Wizard(
                        target.Id,
                        targetX,
                        targetY,
                        target.SpeedX,
                        target.SpeedY,
                        target.Angle,
                        target.Faction,
                        target.Radius,
                        target.Life,
                        target.MaxLife,
                        target.Statuses,
                        target.OwnerPlayerId,
                        target.IsMe,
                        target.Mana,
                        target.MaxMana,
                        target.VisionRange,
                        target.CastRange,
                        target.Xp,
                        target.Level,
                        target.Skills,
                        target.RemainingActionCooldownTicks,
                        target.RemainingCooldownTicksByAction,
                        target.IsMaster,
                        target.Messages);
                }

                var canShootWithMissle = CanShootWithMissle(wizard, startX, startY, newTarget, turnTime);
                if (canShootWithMissle) return true;

            }

            var building = source as Building;
            if (building != null)
            {
                var nearFriends = friends.Where(f => building.GetDistanceTo(f) <= building.AttackRange);
                if (building.GetDistanceTo(target) - target.Radius <= building.AttackRange && nearFriends.Count() <= 1)
                    return true;
            }


            //if (minion != null && minion.Type == MinionType.OrcWoodcutter)
            //{
            //    var estTime =
            //}

            return false;

            //var Wizard = unit as Wizard;


        }

        /// <summary>
        /// Можно ли попасть в волшебника пулей MagicMissile
        /// </summary>
        /// <param name="wizard">стреляющий волшебник</param>
        /// <param name="startX">стартовый X стрельбы</param>
        /// <param name="startY">стартовый X стрельбы</param>
        /// <param name="target">цель стрельбы</param>
        /// <param name="turnTime">время поворота до цели</param>
        /// <returns></returns>
        private bool CanShootWithMissle(Wizard wizard, double startX, double startY, Wizard target, double turnTime)
        {

            var bsd = new BulletStartData(
                startX,
                startY,
                wizard.CastRange,
                target.X - startX,
                target.Y - startY,
                _game.MagicMissileRadius,
                _game.MagicMissileSpeed);


            var bulletTime = GetBulletTime(bsd, target);
            //var thisTickTime = dist/_game.MagicMissileSpeed + minion.RemainingActionCooldownTicks + turnTime;
            var nextTickTime = bulletTime +
                               wizard.RemainingCooldownTicksByAction[(int)ActionType.MagicMissile] + turnTime;

            var canGoBack = CanGoBack(target, bsd, nextTickTime, true);
            return !canGoBack;
        }

        private bool CanShootWithFrostBolt(Wizard wizard, double startX, double startY, Wizard target, double turnTime)
        {

            var bsd = new BulletStartData(
                startX,
                startY,
                wizard.CastRange,
                target.X - startX,
                target.Y - startY,
                _game.FrostBoltRadius,
                _game.FrostBoltSpeed);


            var bulletTime = GetBulletTime(bsd, target);
            //var thisTickTime = dist/_game.MagicMissileSpeed + minion.RemainingActionCooldownTicks + turnTime;
            var nextTickTime = bulletTime +
                               wizard.RemainingCooldownTicksByAction[(int)ActionType.FrostBolt] + turnTime;

            var canGoBack = CanGoBack(target, bsd, nextTickTime, true);
            return !canGoBack;
        }

        private bool CanShootWithFireball(Wizard wizard, double startX, double startY, Wizard target, double turnTime)
        {

            var bsd = new BulletStartData(
                startX,
                startY,
                wizard.CastRange,
                target.X - startX,
                target.Y - startY,
                _game.FireballRadius,
                _game.FireballSpeed);


            var bulletTime = GetBulletTime(bsd, target);
            //var thisTickTime = dist/_game.MagicMissileSpeed + minion.RemainingActionCooldownTicks + turnTime;
            var nextTickTime = bulletTime +
                               wizard.RemainingCooldownTicksByAction[(int)ActionType.Fireball] + turnTime;

            var canGoBack = CanGoBack(target, bsd, nextTickTime, true);
            return !canGoBack;
        }


        private int GetBulletTime(BulletStartData bulletStartData, Wizard target)
        {
            Point2D currBulletPoint;
            int t;
            double bulletAngle;
            var currTargetPoint = new Point2D(target.X, target.Y);

            if (bulletStartData.Bullet != null)
            {
                currBulletPoint = new Point2D(bulletStartData.Bullet.X, bulletStartData.Bullet.Y);
                t = 0;
                bulletAngle = bulletStartData.Bullet.Angle;
            }
            else
            {
                currBulletPoint = new Point2D(bulletStartData.StartX, bulletStartData.StartY);
                t = 0;
                bulletAngle = bulletStartData.Angle;
            }


            var isIn = currTargetPoint.getDistanceTo(currBulletPoint) < target.Radius + bulletStartData.Radius ||
                       currBulletPoint.getDistanceTo(bulletStartData.StartX, bulletStartData.StartY) >
                       currTargetPoint.getDistanceTo(bulletStartData.StartX, bulletStartData.StartY);
            while (!isIn)
            {
                t++;
                var newTargetX = currTargetPoint.X - GetWizardMaxBackSpeed(target) * Math.Cos(target.Angle);
                var newTargetY = currTargetPoint.Y - GetWizardMaxBackSpeed(target) * Math.Sin(target.Angle);
                currTargetPoint = new Point2D(newTargetX, newTargetY);

                var newBulletX = currBulletPoint.X + bulletStartData.Speed * Math.Cos(bulletAngle);
                var newBulletY = currBulletPoint.Y + bulletStartData.Speed * Math.Sin(bulletAngle);
                currBulletPoint = new Point2D(newBulletX, newBulletY);

                isIn = currTargetPoint.getDistanceTo(currBulletPoint) < target.Radius + bulletStartData.Radius ||
                      currBulletPoint.getDistanceTo(bulletStartData.StartX, bulletStartData.StartY) >
                       currTargetPoint.getDistanceTo(bulletStartData.StartX, bulletStartData.StartY); ;
            }
            return t;
        }

        private BulletStartData GetBulletFlyingInMe()
        {
            foreach (var key in _bulletStartDatas.Keys)
            {
                var isIntersect = Square.Intersect(
                    _bulletStartDatas[key].StartX,
                    _bulletStartDatas[key].StartY,
                    _bulletStartDatas[key].EndX,
                    _bulletStartDatas[key].EndY,
                    _self.X,
                    _self.Y,
                    _bulletStartDatas[key].Radius,
                    _self.Radius + GetWizardMaxForwardSpeed(_self));

                if (isIntersect) return _bulletStartDatas[key];
            }
            return null;
        }

        private bool MakeBonusPath(Point2D bonusPoint, bool needTurn)
        {
            var x1 = bonusPoint.X - _game.BonusRadius - _self.Radius;
            var y1 = bonusPoint.Y - _game.BonusRadius - _self.Radius;
            var x2 = bonusPoint.X + _game.BonusRadius + _self.Radius;
            var y2 = bonusPoint.Y + _game.BonusRadius + _self.Radius;

            var i1 = GetSquareI(x1);
            if (i1 < 0) i1 = 0;
            var j1 = GetSquareJ(y1);
            if (j1 < 0) j1 = 0;
            var i2 = GetSquareI(x2);
            if (i2 > _n - 1) i2 = _n - 1;
            var j2 = GetSquareJ(y2);
            if (j2 > _m - 1) j2 = _m - 1;

            for (int i = i1; i <= i2 && i < _n; ++i)
            {
                for (int j = j1; j <= j2 && j < _m; ++j)
                {
                    _table[i, j].Weight = 999999;
                }
            }

            var resPoint = new Point2D(2 * bonusPoint.X - _self.X, 2 * bonusPoint.Y - _self.Y);

            _thisTickResPoint = new Point2D(resPoint.X, resPoint.Y);
            return goTo(resPoint, _self.Radius * 2, _self.Radius * 2, needTurn);
        }

        private bool MakeBonus1Path(bool needTurn)
        {
            var x1 = _bonusPoints[1].X - _game.BonusRadius - _self.Radius;
            var y1 = _bonusPoints[1].Y - _game.BonusRadius - _self.Radius;
            var x2 = _bonusPoints[1].X + _game.BonusRadius + _self.Radius;
            var y2 = _bonusPoints[1].Y + _game.BonusRadius + _self.Radius;

            var i1 = GetSquareI(x1);
            if (i1 < 0) i1 = 0;
            var j1 = GetSquareJ(y1);
            if (j1 < 0) j1 = 0;
            var i2 = GetSquareI(x2);
            if (i2 > _n - 1) i2 = _n - 1;
            var j2 = GetSquareJ(y2);
            if (j2 > _m - 1) j2 = _m - 1;

            for (int i = i1; i <= i2 && i < _n; ++i)
            {
                for (int j = j1; j <= j2 && j < _m; ++j)
                {
                    _table[i, j].Weight = 999999;
                }
            }

            var resPoint = new Point2D(2 * _bonusPoints[1].X - _self.X, 2 * _bonusPoints[1].Y - _self.Y);

            _thisTickResPoint = new Point2D(resPoint.X, resPoint.Y);
            return goTo(resPoint, _self.Radius * 2, _self.Radius * 2, needTurn);
        }


        private Wizard GetClosestToBonusWizard(Point2D bonusPoint)
        {
            var wizards = _world.Wizards.Where(x => !x.IsMe);
            var selfDist = bonusPoint.getDistanceTo(_self);

            var minDist = double.MaxValue;
            Wizard nearestWizard = null;

            foreach (var wizard in wizards)
            {
                var currDist = bonusPoint.getDistanceTo(wizard);
                if (currDist > selfDist && currDist < minDist)
                {
                    minDist = currDist;
                    nearestWizard = wizard;
                }
            }
            return nearestWizard;
        }



        private bool NeedGoBack()
        {
            return _self.Life < _self.MaxLife * LOW_HP_FACTOR && IsInDangerousArea(_self, _self.Radius * 2);

            //if (_self.Life < _self.MaxLife * LOW_HP_FACTOR && IsInDangerousArea(_self, _self.Radius * 2)) return true;
            //if (canGoOnStaffRange && _self.Life >= _self.MaxLife * LOW_HP_BONUS_FACTOR) return false;

            //var bsd = GetBulletFlyingInMe();
            //if (bsd != null) return true;

            //var isInDangerousArea = IsInDangerousArea(_self, -_self.Radius/2);
            //if (!isInDangerousArea) return false;


            //var remainingCooldown = _self.RemainingCooldownTicksByAction[(int)ActionType.MagicMissile];
            //LivingUnit target = null;
            //var shootingTarget = GetShootingTarget();
            //if (shootingTarget != null)
            //{
            //    target = shootingTarget;
            //}
            //else
            //{
            //    var nearestToBaseTarget = GetNearestMyBaseAnemy();
            //    if (nearestToBaseTarget != null) target = nearestToBaseTarget;
            //}
            //if (target == null) return false;

            //var delta = _game.WizardForwardSpeed * (remainingCooldown - 2);
            //var stillCanShoot = IsOkDistanceToShoot(_self, target, delta);
            //return stillCanShoot;


        }

        private bool IsSquareVisible(Square square)
        {
            foreach (var unit in _world.Buildings.Where(x => x.Faction == _self.Faction))
            {
                var farX = unit.X > square.X + square.Side / 2 ? square.X : square.X + square.Side;
                var farY = unit.Y > square.Y + square.Side / 2 ? square.Y : square.Y + square.Side;

                var dist = unit.GetDistanceTo(farX, farY);
                if (dist <= unit.VisionRange)
                {
                    return true;
                }
            }
            foreach (var unit in _world.Wizards.Where(x => x.Faction == _self.Faction))
            {
                var farX = unit.X > square.X + square.Side / 2 ? square.X : square.X + square.Side;
                var farY = unit.Y > square.Y + square.Side / 2 ? square.Y : square.Y + square.Side;

                var dist = unit.GetDistanceTo(farX, farY);
                if (dist <= unit.VisionRange)
                {
                    return true;
                }
            }
            foreach (var unit in _world.Minions.Where(x => x.Faction == _self.Faction))
            {
                var farX = unit.X > square.X + square.Side / 2 ? square.X : square.X + square.Side;
                var farY = unit.Y > square.Y + square.Side / 2 ? square.Y : square.Y + square.Side;

                var dist = unit.GetDistanceTo(farX, farY);
                if (dist <= unit.VisionRange)
                {
                    return true;
                }
            }
            return false;
        }

        private void MakeDinamycAStar()
        {
            _squares = new List<Square>();
            _squareSize = _self.Radius;
            _n = (int)(_world.Width / _squareSize);
            _m = (int)(_world.Height / _squareSize);

            _table = new Square[_n, _m];

            for (int i = 0; i < _n; ++i)
            {
                for (int j = 0; j < _m; ++j)
                {
                    var square = new Square(
                         _squareSize,
                         i * _squareSize,
                         j * _squareSize,
                         1d,
                         i + ":" + j,
                         _game);

                    _squares.Add(square);

                    _table[i, j] = square;
                }
            }

            for (int i = 0; i < _n; ++i)
            {
                for (int j = 0; j < _m; ++j)
                {
                    var neighbors = new List<Square>();
                    if (i != 0)
                    {
                        neighbors.Add(_table[i - 1, j]);
                    }
                    if (i != _n - 1)
                    {
                        neighbors.Add(_table[i + 1, j]);
                    }
                    if (j != 0)
                    {
                        neighbors.Add(_table[i, j - 1]);
                    }
                    if (j != _m - 1)
                    {
                        neighbors.Add(_table[i, j + 1]);
                    }

                    if (i != 0 && j != 0)
                    {
                        neighbors.Add(_table[i - 1, j - 1]);
                    }

                    if (i != _n - 1 && j != _m - 1)
                    {
                        neighbors.Add(_table[i + 1, j + 1]);
                    }

                    if (i != 0 && j != _m - 1)
                    {
                        neighbors.Add(_table[i - 1, j + 1]);
                    }

                    if (i != _n - 1 && j != 0)
                    {
                        neighbors.Add(_table[i + 1, j - 1]);
                    }

                    var square = _table[i, j];
                    square.Neighbors = neighbors;
                }
            }



        }


        private void RemoveCutTrees()
        {
            foreach (var unit in _world.Minions.Where(x => x.Faction == _self.Faction))
            {
                _trees.RemoveWhere(
                    t => unit.GetDistanceTo(t) <= unit.VisionRange && _world.Trees.All(tt => tt.Id != t.Id));
            }
            foreach (var unit in _world.Wizards.Where(x => x.Faction == _self.Faction))
            {
                _trees.RemoveWhere(
                    t => unit.GetDistanceTo(t) <= unit.VisionRange && _world.Trees.All(tt => tt.Id != t.Id));
            }
            foreach (var unit in _world.Buildings.Where(x => x.Faction == _self.Faction))
            {
                _trees.RemoveWhere(
                    t => unit.GetDistanceTo(t) <= unit.VisionRange && _world.Trees.All(tt => tt.Id != t.Id));
            }
        }


        private void UpdateDinamycAStar()
        {
            RemoveCutTrees();
            foreach (var tree in _world.Trees)
            {
                if (!_trees.Contains(tree)) _trees.Add(tree);
            }


            var myLeftX = _self.X - _squareSize / 2;
            var leftN = (int)(myLeftX / _squareSize);
            _startX = myLeftX - leftN * _squareSize;

            var myTopY = _self.Y - _squareSize / 2;
            var topM = (int)(myTopY / _squareSize);
            _startY = myTopY - topM * _squareSize;


            var units = new List<LivingUnit>();
            units.AddRange(_world.Buildings);
            units.AddRange(_world.Minions);
            units.AddRange(_world.Wizards.Where(x => !x.IsMe));
            units.AddRange(_trees);


            for (int i = 0; i < _n; ++i)
            {
                for (int j = 0; j < _m; ++j)
                {
                    var square = _table[i, j];
                    square.X = _startX + i * _squareSize;
                    square.Y = _startY + j * _squareSize;
                    square.Units = null;
                    square.Weight = 1d;
                    //_table[i, j] = new Square(_squareSize, _startX + i*_squareSize, _startY + j*_squareSize, 1d, "");
                }
            }

            _startSquare = _table[leftN, topM];




            for (int k = 0; k < _anemyBuildings.Count; ++k)
            {
                if (!_IsAnemyBuildingAlive[k]) continue;
                var isBuildingExists = _world.Buildings.Any(b => Math.Abs(b.X - _anemyBuildings[k].X) < TOLERANCE && Math.Abs(b.Y - _anemyBuildings[k].Y) < TOLERANCE);
                if (isBuildingExists) continue;

                foreach (var unit in _world.Wizards.Where(x => x.Faction == _self.Faction))
                {
                    if (unit.VisionRange > unit.GetDistanceTo(_anemyBuildings[k].X, _anemyBuildings[k].Y))
                    {
                        _IsAnemyBuildingAlive[k] = false;
                        break;
                    }
                }

                foreach (var unit in _world.Minions.Where(x => x.Faction == _self.Faction))
                {
                    if (unit.VisionRange > unit.GetDistanceTo(_anemyBuildings[k].X, _anemyBuildings[k].Y))
                    {
                        _IsAnemyBuildingAlive[k] = false;
                        break;
                    }
                }

            }

            //_isVisibleSquares = new bool[_n, _m];


            //foreach (var unit in _world.Buildings.Where(x => x.Faction == _self.Faction))
            //{
            //    var x1 = unit.X - unit.VisionRange;
            //    var y1 = unit.Y - unit.VisionRange;
            //    var x2 = unit.X + unit.VisionRange;
            //    var y2 = unit.Y + unit.VisionRange;

            //    var i1 = GetSquareI(x1);
            //    if (i1 < 0) i1 = 0;
            //    var j1 = GetSquareJ(y1);
            //    if (j1 < 0) j1 = 0;
            //    var i2 = GetSquareI(x2);
            //    if (i2 > _n - 1) i2 = _n - 1;
            //    var j2 = GetSquareJ(y2);
            //    if (i2 > _m - 1) j2 = _m - 1;

            //    for (int i = i1; i <= i2 && i < _n; ++i)
            //    {
            //        for (int j = j1; j <= j2 && j < _m; ++j)
            //        {
            //            if (_isVisibleSquares[i, j]) continue;

            //            var centerX = _startX + i * _squareSize + _squareSize / 2;
            //            var centerY = _startY + j * _squareSize + _squareSize / 2;
            //            var dist = unit.GetDistanceTo(centerX, centerY);
            //            if (dist <= unit.VisionRange)
            //            {
            //                _isVisibleSquares[i, j] = true;
            //            }
            //        }
            //    }
            //}
            //foreach (var unit in _world.Wizards.Where(x => x.Faction == _self.Faction))
            //{
            //    var x1 = unit.X - unit.VisionRange;
            //    var y1 = unit.Y - unit.VisionRange;
            //    var x2 = unit.X + unit.VisionRange;
            //    var y2 = unit.Y + unit.VisionRange;

            //    var i1 = GetSquareI(x1);
            //    if (i1 < 0) i1 = 0;
            //    var j1 = GetSquareJ(y1);
            //    if (j1 < 0) j1 = 0;
            //    var i2 = GetSquareI(x2);
            //    if (i2 > _n - 1) i2 = _n - 1;
            //    var j2 = GetSquareJ(y2);
            //    if (i2 > _m - 1) j2 = _m - 1;

            //    for (int i = i1; i <= i2 && i < _n; ++i)
            //    {
            //        for (int j = j1; j <= j2 && j < _m; ++j)
            //        {
            //            if (_isVisibleSquares[i, j]) continue;

            //            var centerX = _startX + i * _squareSize + _squareSize / 2;
            //            var centerY = _startY + j * _squareSize + _squareSize / 2;
            //            var dist = unit.GetDistanceTo(centerX, centerY);
            //            if (dist <= unit.VisionRange)
            //            {
            //                _isVisibleSquares[i, j] = true;
            //            }
            //        }
            //    }
            //}
            //foreach (var unit in _world.Minions.Where(x => x.Faction == _self.Faction))
            //{
            //    var x1 = unit.X - unit.VisionRange;
            //    var y1 = unit.Y - unit.VisionRange;
            //    var x2 = unit.X + unit.VisionRange;
            //    var y2 = unit.Y + unit.VisionRange;

            //    var i1 = GetSquareI(x1);
            //    if (i1 < 0) i1 = 0;
            //    var j1 = GetSquareJ(y1);
            //    if (j1 < 0) j1 = 0;
            //    var i2 = GetSquareI(x2);
            //    if (i2 > _n - 1) i2 = _n - 1;
            //    var j2 = GetSquareJ(y2);
            //    if (i2 > _m - 1) j2 = _m - 1;

            //    for (int i = i1; i <= i2 && i < _n; ++i)
            //    {
            //        for (int j = j1; j <= j2 && j < _m; ++j)
            //        {
            //            if (_isVisibleSquares[i, j]) continue;

            //            var centerX = _startX + i * _squareSize + _squareSize / 2;
            //            var centerY = _startY + j * _squareSize + _squareSize / 2;
            //            var dist = unit.GetDistanceTo(centerX, centerY);
            //            if (dist <= unit.VisionRange)
            //            {
            //                _isVisibleSquares[i, j] = true;
            //            }
            //        }
            //    }
            //}

            //for (int i = 0; i < _n; ++i)
            //{
            //    for (int j = 0; j < _m; ++j)
            //    {
            //        if (_isVisibleSquares[i, j]) continue;
            //        _table[i, j].Weight = 999999;
            //    }
            //}


            //for (int i = 0; i < _n; ++i)
            //{
            //    for (int j = 0; j < _m; ++j)
            //    {
            //        var square = _table[i, j];

            //        var x = square.X + square.Side / 2;
            //        var y = square.Y + square.Side / 2;

            //        if (x <= ROW_WIDTH || y <= ROW_WIDTH ||
            //            x >= _world.Width - ROW_WIDTH || y >= _world.Height - ROW_WIDTH)
            //        {
            //            square.Weight = 1;
            //        }
            //        else
            //        {
            //            if (y >= x - ROW_WIDTH && y <= x + ROW_WIDTH ||
            //                y >= _world.Width - ROW_WIDTH - x && y <= _world.Width + ROW_WIDTH - x)
            //            {
            //                square.Weight = 1;
            //            }
            //        }
            //    }
            //}

            foreach (var unit in units)
            {
                var x1 = unit.X - unit.Radius - _self.Radius;
                var y1 = unit.Y - unit.Radius - _self.Radius;
                var x2 = unit.X + unit.Radius + _self.Radius;
                var y2 = unit.Y + unit.Radius + _self.Radius;

                if (_lastTickPath != null)
                {
                    foreach (Square p in _lastTickPath)
                    {
                        if (p.X + p.Side >= x1 && p.X <= x2 && p.Y + p.Side >= y1 && p.Y <= y2)
                        {
                            var centerX = p.X + _squareSize / 2;
                            var centerY = p.Y + _squareSize / 2;
                            var dist = unit.GetDistanceTo(centerX, centerY);

                            if (!(unit is Tree) && dist < unit.Radius + _self.Radius)
                            {
                                p.Weight = 999999;
                            }
                            else
                            {
                                if (p.Units == null) p.Units = new HashSet<LivingUnit>();
                                p.Units.Add(unit);
                            }
                        }
                    }
                }

                var i1 = GetSquareI(x1);
                if (i1 < 0) i1 = 0;
                var j1 = GetSquareJ(y1);
                if (j1 < 0) j1 = 0;
                var i2 = GetSquareI(x2);
                if (i2 > _n - 1) i2 = _n - 1;
                var j2 = GetSquareJ(y2);
                if (j2 > _m - 1) j2 = _m - 1;

                for (int i = i1; i <= i2 && i < _n; ++i)
                {
                    for (int j = j1; j <= j2 && j < _m; ++j)
                    {
                        if (_table[i, j].Weight == 999999) continue;

                        var centerX = _startX + i * _squareSize + _squareSize / 2;
                        var centerY = _startY + j * _squareSize + _squareSize / 2;
                        var dist = unit.GetDistanceTo(centerX, centerY);

                        if (!(unit is Tree) && dist < unit.Radius + _self.Radius)
                        {
                            _table[i, j].Weight = 999999;
                        }
                        else
                        {
                            if (_table[i, j].Units == null) _table[i, j].Units = new HashSet<LivingUnit>();
                            _table[i, j].Units.Add(unit);
                        }

                    }

                }
            }

            for (int k = 0; k < _anemyBuildings.Count; ++k)
            {
                if (!_IsAnemyBuildingAlive[k]) continue;

                var x1 = _anemyBuildings[k].X - _anemyBuildings[k].AttackRange;
                var y1 = _anemyBuildings[k].Y - _anemyBuildings[k].AttackRange;
                var x2 = _anemyBuildings[k].X + _anemyBuildings[k].AttackRange;
                var y2 = _anemyBuildings[k].Y + _anemyBuildings[k].AttackRange;

                if (_lastTickPath != null)
                {
                    foreach (Square p in _lastTickPath)
                    {
                        if (p.Weight > 1) continue;

                        if (p.X + p.Side >= x1 && p.X <= x2 && p.Y + p.Side >= y1 && p.Y <= y2)
                        {
                            var centerX = p.X + _squareSize / 2;
                            var centerY = p.Y + _squareSize / 2;
                            var dist = _anemyBuildings[k].GetDistanceTo(centerX, centerY);
                            if (dist < _anemyBuildings[k].AttackRange)
                            {
                                p.Weight += SHOOTING_SQUARE_WEIGHT;
                            }
                        }
                    }
                }

                var i1 = GetSquareI(x1);
                if (i1 < 0) i1 = 0;
                var j1 = GetSquareJ(y1);
                if (j1 < 0) j1 = 0;
                var i2 = GetSquareI(x2);
                if (i2 > _n - 1) i2 = _n - 1;
                var j2 = GetSquareJ(y2);
                if (i2 > _m - 1) j2 = _m - 1;

                for (int i = i1; i <= i2 && i < _n; ++i)
                {
                    for (int j = j1; j <= j2 && j < _m; ++j)
                    {
                        if (_table[i, j].Weight > 1) continue;

                        var centerX = _startX + i * _squareSize + _squareSize / 2;
                        var centerY = _startY + j * _squareSize + _squareSize / 2;
                        var dist = _anemyBuildings[k].GetDistanceTo(centerX, centerY);
                        if (dist < _anemyBuildings[k].AttackRange)
                        {
                            _table[i, j].Weight += SHOOTING_SQUARE_WEIGHT;
                        }
                    }
                }
            }

            foreach (var w in _world.Wizards.Where(x => x.Faction != _self.Faction))
            {
                var x1 = w.X - w.CastRange;
                var y1 = w.Y - w.CastRange;
                var x2 = w.X + w.CastRange;
                var y2 = w.Y + w.CastRange;

                if (_lastTickPath != null)
                {
                    foreach (Square p in _lastTickPath)
                    {
                        if (p.Weight > 1) continue;

                        if (p.X + p.Side >= x1 && p.X <= x2 && p.Y + p.Side >= y1 && p.Y <= y2)
                        {
                            var centerX = p.X + _squareSize / 2;
                            var centerY = p.Y + _squareSize / 2;
                            var dist = w.GetDistanceTo(centerX, centerY);
                            if (dist < w.CastRange)
                            {
                                p.Weight += SHOOTING_SQUARE_WEIGHT;
                            }
                        }
                    }
                }

                var i1 = GetSquareI(x1);
                if (i1 < 0) i1 = 0;
                var j1 = GetSquareJ(y1);
                if (j1 < 0) j1 = 0;
                var i2 = GetSquareI(x2);
                if (i2 > _n - 1) i2 = _n - 1;
                var j2 = GetSquareJ(y2);
                if (i2 > _m - 1) j2 = _m - 1;

                for (int i = i1; i <= i2 && i < _n; ++i)
                {
                    for (int j = j1; j <= j2 && j < _m; ++j)
                    {
                        if (_table[i, j].Weight > 1) continue;

                        var centerX = _startX + i * _squareSize + _squareSize / 2;
                        var centerY = _startY + j * _squareSize + _squareSize / 2;
                        var dist = w.GetDistanceTo(centerX, centerY);
                        if (dist < w.CastRange)
                        {
                            _table[i, j].Weight += SHOOTING_SQUARE_WEIGHT;
                        }
                    }
                }
            }


            var leftI = 0;
            while (leftI * _squareSize + _squareSize / 2 < _self.Radius)
            {
                leftI++;
            }
            for (int i = 0; i < leftI; ++i)
            {
                for (int j = 0; j < _m; ++j)
                {
                    _table[i, j].Weight = 999999;
                }
            }

            var rightI = _n - 1;
            while (_world.Width - (rightI * _squareSize + _squareSize / 2) < _self.Radius)
            {
                rightI--;
            }
            for (int i = rightI; i < _n; ++i)
            {
                for (int j = 0; j < _m; ++j)
                {
                    _table[i, j].Weight = 999999;
                }
            }

            var topJ = 0;
            while (topJ * _squareSize + _squareSize / 2 < _self.Radius)
            {
                topJ++;
            }
            for (int i = 0; i < _n; ++i)
            {
                for (int j = 0; j < topJ; ++j)
                {
                    _table[i, j].Weight = 999999;
                }
            }

            var bottomJ = _m - 1;
            while (_world.Height - (bottomJ * _squareSize + _squareSize / 2) < _self.Radius)
            {
                bottomJ--;
            }
            for (int i = 0; i < _n; ++i)
            {
                for (int j = bottomJ; j < _m; ++j)
                {
                    _table[i, j].Weight = 999999;
                }
            }


            //            for (int i = 0; i < _n; ++i)
            //            {
            //                for (int j = 0; j < _m; ++j)
            //                {
            //                    var square = _table[i, j];
            //
            //                    if (square.X <= ROW_WIDTH * 2 && square.Y >= _world.Height - ROW_WIDTH * 2) continue;
            //                    if (square.Y <= ROW_WIDTH * 2 && square.X >= _world.Width - ROW_WIDTH * 2) continue;
            //
            //                    if (_line == LaneType.Top && square.Y > _world.Width - ROW_WIDTH - square.X)
            //                    {
            //                        square.Weight = 999999;
            //                    }
            //                    if (_line == LaneType.Bottom && square.Y < _world.Width + ROW_WIDTH - square.X)
            //                    {
            //                        square.Weight = 999999;
            //                    }
            //                    if (_line == LaneType.Middle && (square.X <= ROW_WIDTH || square.Y <= ROW_WIDTH ||
            //                                                     square.X >= _world.Width - ROW_WIDTH ||
            //                                                     square.Y >= _world.Height - ROW_WIDTH))
            //                    {
            //                        square.Weight = 999999;
            //                    }
            //                }
            //            }
        }

        //private void MakeStaticAStar()
        //{
        //    _staticSquares = new List<Square>();
        //    _staticSquareSize = _self.Radius*2;
        //    _staticN = (int) Math.Truncate(_world.Width/_squareSize);
        //    _staticM = (int) Math.Truncate(_world.Height/_squareSize);
        //    _staticTable = new Square[_staticN, _staticM];


        //    _staticStartX = (_world.Width - _staticN * _staticSquareSize) /2;
        //    _staticStartY = (_world.Height - _staticM * _staticSquareSize) /2;

        //    for (int i = 0; i < _staticN; ++i)
        //    {
        //        for (int j = 0; j < _staticM; ++j)
        //        {
        //            var square = new Square(
        //                _staticSquareSize,
        //                _staticStartX + i* _staticSquareSize,
        //                _staticStartY + j* _staticSquareSize,
        //                999999d,
        //                i + ":" + j, 
        //                _game);

        //            _staticSquares.Add(square);

        //            _staticTable[i, j] = square;
        //        }
        //    }




        //    for (int i = 0; i < _staticN; ++i)
        //    {
        //        for (int j = 0; j < _staticM; ++j)
        //        {
        //            var neighbors = new List<Square>();
        //            if (i != 0)
        //            {
        //                neighbors.Add(_staticTable[i - 1, j]);
        //            }
        //            if (i != _n - 1)
        //            {
        //                neighbors.Add(_staticTable[i + 1, j]);
        //            }
        //            if (j != 0)
        //            {
        //                neighbors.Add(_staticTable[i, j - 1]);
        //            }
        //            if (j != _m - 1)
        //            {
        //                neighbors.Add(_staticTable[i, j + 1]);
        //            }

        //            if (i != 0 && j != 0)
        //            {
        //                neighbors.Add(_staticTable[i - 1, j - 1]);
        //            }

        //            if (i != _n - 1 && j != _m - 1)
        //            {
        //                neighbors.Add(_staticTable[i + 1, j + 1]);
        //            }

        //            if (i != 0 && j != _m - 1)
        //            {
        //                neighbors.Add(_staticTable[i - 1, j + 1]);
        //            }

        //            if (i != _n - 1 && j != 0)
        //            {
        //                neighbors.Add(_staticTable[i + 1, j - 1]);
        //            }

        //            var square = _staticTable[i, j];

        //            square.Neighbors = neighbors;
        //        }
        //    }

        //    for (int i = 0; i < _staticN; ++i)
        //    {
        //        for (int j = 0; j < _staticM; ++j)
        //        {
        //            var square = _staticTable[i, j];

        //            var x = square.X + square.Side / 2;
        //            var y = square.Y + square.Side / 2;

        //            if (x <= ROW_WIDTH || y <= ROW_WIDTH ||
        //                x >= _world.Width - ROW_WIDTH || y >= _world.Height - ROW_WIDTH)
        //            {
        //                square.Weight = 1;
        //            }
        //            else
        //            {
        //                if (y >= x - ROW_WIDTH && y <= x + ROW_WIDTH ||
        //                    y >= _world.Width - ROW_WIDTH - x && y <= _world.Width + ROW_WIDTH - x)
        //                {
        //                    square.Weight = 1;
        //                }
        //            }
        //        }
        //    }
        //}


        private void UpdateStaticAStar()
        {
            var startSquareI = GetSquareI(_self.X);
            var startSquareJ = GetSquareJ(_self.Y);

            _startSquare = _table[startSquareI, startSquareJ];


            for (int k = 0; k < _myBuildings.Count; ++k)
            {
                if (!_IsMyBuildingAlive[k]) continue;
                if (!_world.Buildings.Any(b => b.X == _myBuildings[k].X && b.Y == _myBuildings[k].Y))
                {
                    _IsMyBuildingAlive[k] = false;

                    var x1 = _myBuildings[k].X - _myBuildings[k].Radius;
                    var y1 = _myBuildings[k].Y - _myBuildings[k].Radius;
                    var x2 = _myBuildings[k].X + _myBuildings[k].Radius;
                    var y2 = _myBuildings[k].Y + _myBuildings[k].Radius;

                    var i1 = GetSquareI(x1);
                    var j1 = GetSquareJ(y1);
                    var i2 = GetSquareI(x2);
                    var j2 = GetSquareJ(y2);

                    for (int i = i1; i <= i2 && i < _n; ++i)
                    {
                        for (int j = j1; j <= j2 && j < _m; ++j)
                        {
                            _traversedSquares[i, j] = false;
                        }
                    }
                }
            }


            for (int k = 0; k < _anemyBuildings.Count; ++k)
            {
                if (!_IsAnemyBuildingAlive[k]) continue;
                var isBuildingExists = _world.Buildings.Any(b => b.X == _anemyBuildings[k].X && b.Y == _anemyBuildings[k].Y);
                if (isBuildingExists) continue;

                foreach (var unit in _world.Wizards.Where(x => x.Faction == _self.Faction))
                {
                    if (unit.VisionRange > unit.GetDistanceTo(_anemyBuildings[k].X, _anemyBuildings[k].Y))
                    {
                        _IsAnemyBuildingAlive[k] = false;
                        break;
                    }
                }

                foreach (var unit in _world.Minions.Where(x => x.Faction == _self.Faction))
                {
                    if (unit.VisionRange > unit.GetDistanceTo(_anemyBuildings[k].X, _anemyBuildings[k].Y))
                    {
                        _IsAnemyBuildingAlive[k] = false;
                        break;
                    }
                }


                if (!_IsAnemyBuildingAlive[k])
                {
                    var x1 = _anemyBuildings[k].X - _anemyBuildings[k].Radius;
                    var y1 = _anemyBuildings[k].Y - _anemyBuildings[k].Radius;
                    var x2 = _anemyBuildings[k].X + _anemyBuildings[k].Radius;
                    var y2 = _anemyBuildings[k].Y + _anemyBuildings[k].Radius;

                    var i1 = GetSquareI(x1);
                    var j1 = GetSquareJ(y1);
                    var i2 = GetSquareI(x2);
                    var j2 = GetSquareJ(y2);

                    for (int i = i1; i <= i2 && i < _n; ++i)
                    {
                        for (int j = j1; j <= j2 && j < _m; ++j)
                        {
                            _traversedSquares[i, j] = false;
                        }
                    }
                }
            }

            for (int i = 0; i < _n; ++i)
            {
                for (int j = 0; j < _m; ++j)
                {
                    var square = _table[i, j];
                    if (_traversedSquares[i, j])
                    {
                        square.Weight = 999999;
                    }
                    else if (!IsSquareVisible(square))
                    {
                        var x = square.X + square.Side / 2;
                        var y = square.Y + square.Side / 2;

                        if (x <= ROW_WIDTH || y <= ROW_WIDTH ||
                            x >= _world.Width - ROW_WIDTH || y >= _world.Height - ROW_WIDTH)
                        {
                            square.Weight = DEFAULT_WEIGHT;
                        }
                        else
                        {
                            var pos1 =
                                GetPointPosition(
                                    new Vector()
                                    {
                                        P1 = new Point2D(0, _world.Height - ROW_WIDTH),
                                        P2 = new Point2D(_world.Width - ROW_WIDTH, 0)
                                    }, x, y);

                            var pos2 =
                                GetPointPosition(
                                    new Vector()
                                    {
                                        P1 = new Point2D(0, _world.Height + ROW_WIDTH),
                                        P2 = new Point2D(_world.Width + ROW_WIDTH, 0)
                                    }, x, y);

                            var pos3 =
                                GetPointPosition(
                                    new Vector()
                                    {
                                        P1 = new Point2D(0, ROW_WIDTH),
                                        P2 = new Point2D(_world.Width - ROW_WIDTH, _world.Height)
                                    }, x, y);

                            var pos4 =
                                GetPointPosition(
                                    new Vector()
                                    {
                                        P1 = new Point2D(ROW_WIDTH, 0),
                                        P2 = new Point2D(_world.Width, _world.Height - ROW_WIDTH)
                                    }, x, y);

                            if (pos1 == PointPosition.Right && pos2 == PointPosition.Left ||
                                pos3 == PointPosition.Left && pos4 == PointPosition.Right)
                            {
                                square.Weight = DEFAULT_WEIGHT;
                            }
                            else
                            {
                                square.Weight = 999999;
                            }
                        }
                    }
                    else
                    {
                        square.Weight = DEFAULT_WEIGHT;
                        var minIndex = Math.Min(i, j);
                        if (minIndex <= 3)
                        {
                            square.Weight = DEFAULT_WEIGHT - (4 - minIndex);
                        }
                        var maxIndex = Math.Max(i, j);
                        if (maxIndex >= _n - 4)
                        {
                            square.Weight = DEFAULT_WEIGHT - (4 - (_n - 1 - maxIndex));
                        }
                    }
                }
            }

            var units = new List<LivingUnit>();
            units.AddRange(_world.Wizards.Where(x => !x.IsMe));
            units.AddRange(_world.Minions);
            var newTrees = _world.Trees.Where(t => !_seenTreesIds.Contains(t.Id));
            units.AddRange(newTrees);
            foreach (var tree in newTrees)
            {
                _seenTreesIds.Add(tree.Id);
            }

            foreach (var unit in units)
            {
                var x1 = unit.X - unit.Radius;
                var y1 = unit.Y - unit.Radius;
                var x2 = unit.X + unit.Radius;
                var y2 = unit.Y + unit.Radius;

                var i1 = GetSquareI(x1);
                var j1 = GetSquareJ(y1);
                var i2 = GetSquareI(x2);
                var j2 = GetSquareJ(y2);

                for (int i = i1; i <= i2 && i < _n; ++i)
                {
                    for (int j = j1; j <= j2 && j < _m; ++j)
                    {
                        _table[i, j].Weight = 999999;
                        if (unit is Tree) _traversedSquares[i, j] = true;
                    }
                }
            }


        }



        private Point2D GetRadiusPoint(double borderDist, double radius, LivingUnit target)
        {
            var isOkPos = _self.X <= ROW_WIDTH * 1.25 || _self.Y <= ROW_WIDTH * 1.25 ||
                          _self.X >= _world.Width - ROW_WIDTH * 1.25 ||
                          _self.Y >= _world.Height - ROW_WIDTH * 1.25 ||
                          _self.Y >= _world.Width - 0.75 * ROW_WIDTH - _self.X &&
                          _self.Y <= _world.Width + 0.75 * ROW_WIDTH - _self.X;
            if (isOkPos) return null;

            if (_line == LaneType.Top)
            {
                //if (_self.X > _self.Y)
                Point2D topPoint;
                {
                    var destY = borderDist;
                    var desc = target.X * target.X - (target.X * target.X +
                                                    Math.Pow(destY - target.Y, 2) - radius * radius);
                    if (desc < 0) topPoint = null;
                    else
                    {
                        var destX1 = target.X + Math.Sqrt(desc);
                        var destX2 = target.X - Math.Sqrt(desc);

                        var dist1 = _selfBase.GetDistanceTo(destX1, destY);
                        var dist2 = _selfBase.GetDistanceTo(destX1, destY);

                        topPoint = new Point2D(dist1 < dist2 ? destX1 : destX2, destY);
                    }
                }
                //else
                Point2D leftPoint;
                {
                    var destX = borderDist;
                    var desc = target.Y * target.Y - (target.Y * target.Y +
                                                                  Math.Pow(destX - target.X, 2) - radius * radius);
                    if (desc < 0) leftPoint = null;
                    else
                    {
                        var destY1 = target.Y + Math.Sqrt(desc);
                        var destY2 = target.Y - Math.Sqrt(desc);

                        var dist1 = _selfBase.GetDistanceTo(destX, destY1);
                        var dist2 = _selfBase.GetDistanceTo(destX, destY2);

                        leftPoint = new Point2D(destX, dist1 < dist2 ? destY1 : destY2);
                    }
                }

                if (topPoint != null && leftPoint != null)
                {
                    var topPointDist = topPoint.getDistanceTo(_selfBase);
                    var leftPointDist = leftPoint.getDistanceTo(_selfBase);
                    return topPointDist < leftPointDist ? topPoint : leftPoint;
                }
                else if (topPoint != null) return topPoint;
                else if (leftPoint != null) return leftPoint;
                return null;
            }

            if (_line == LaneType.Bottom)
            {
                Point2D rightPoint;
                //if (_self.X > _self.Y)
                {
                    var destX = _world.Width - borderDist;
                    var desc = target.Y * target.Y - (target.Y * target.Y +
                                                                  Math.Pow(destX - target.X, 2) -
                                                                  radius * radius);
                    if (desc < 0) rightPoint = null;
                    else
                    {
                        var destY1 = target.Y + Math.Sqrt(desc);
                        var destY2 = target.Y - Math.Sqrt(desc);

                        var dist1 = _selfBase.GetDistanceTo(destX, destY1);
                        var dist2 = _selfBase.GetDistanceTo(destX, destY2);

                        rightPoint = new Point2D(destX, dist1 < dist2 ? destY1 : destY2);
                    }
                }
                Point2D bottomPoint;
                //else
                {
                    var destY = _world.Height - borderDist;
                    var desc = target.X * target.X - (target.X * target.X +
                                                                  Math.Pow(destY - target.Y, 2) -
                                                                  radius * radius);
                    if (desc < 0) bottomPoint = null;
                    else
                    {
                        var destX1 = target.X + Math.Sqrt(desc);
                        var destX2 = target.X - Math.Sqrt(desc);

                        var dist1 = _selfBase.GetDistanceTo(destX1, destY);
                        var dist2 = _selfBase.GetDistanceTo(destX1, destY);

                        bottomPoint = new Point2D(dist1 < dist2 ? destX1 : destX2, destY);
                    }
                }

                if (rightPoint != null && bottomPoint != null)
                {
                    var rightPointDist = rightPoint.getDistanceTo(_selfBase);
                    var bottomPointDist = bottomPoint.getDistanceTo(_selfBase);
                    return rightPointDist < bottomPointDist ? rightPoint : bottomPoint;
                }
                else if (rightPoint != null) return rightPoint;
                else if (bottomPoint != null) return bottomPoint;
                return null;
            }

            if (_line == LaneType.Middle)
            {
                var desc = Math.Pow(target.Y - target.X - _world.Height, 2) -
                           2 *
                           (target.X * target.X + _world.Height * _world.Height - 2 * _world.Height * target.Y +
                            target.Y * target.Y -
                            radius * radius);

                if (desc < 0) return null;
                var destX1 = (-target.Y + target.X + _world.Height + Math.Sqrt(desc)) / 2;
                var destX2 = (-target.Y + target.X + _world.Height - Math.Sqrt(desc)) / 2;
                var destY1 = _world.Height - destX1;
                var destY2 = _world.Height - destX2;

                var dist1 = _selfBase.GetDistanceTo(destX1, destY1);
                var dist2 = _selfBase.GetDistanceTo(destX2, destY2);

                return new Point2D(dist1 < dist2 ? destX1 : destX2, dist1 < dist2 ? destY1 : destY2);
            }

            return null;
        }

        private IEnumerable<LivingUnit> GetDangerousAnemies(LivingUnit unit, double eps)
        {
            var anemies = new List<LivingUnit>();
            anemies.AddRange(_world.Wizards.Where(x => x.Faction != unit.Faction));
            anemies.AddRange(_world.Minions.Where(x => x.Faction != unit.Faction));
            for (int i = 0; i < _anemyBuildings.Count; ++i)
            {
                if (!_IsAnemyBuildingAlive[i]) continue;
                anemies.Add(_anemyBuildings[i]);
            }


            var result = new List<LivingUnit>();

            foreach (var anemy in anemies)
            {
                if (IsCalmNeutralMinion(anemy)) continue;

                var canShoot = IsOkDistanceToShoot(anemy, unit, eps);
                if (canShoot) result.Add(anemy);

            }
            return result;
        }

        private bool IsInDangerousArea(LivingUnit unit, double eps)
        {
            return GetDangerousAnemies(unit, eps).Any();
        }

        private Point2D GetRadiusPoint(double borderDist, double radius, LivingUnit nearestTarget, LivingUnit distCompareUnit, bool isCheckCloseToBorder)
        {
            var isTop = isCheckCloseToBorder && _self.Y <= borderDist;
            var isLeft = isCheckCloseToBorder && _self.X <= borderDist;
            var isBottom = isCheckCloseToBorder && _self.Y >= _world.Height - borderDist;
            var isRight = isCheckCloseToBorder && _self.X >= _world.Width - borderDist;


            //var isBadAngleToBase = Math.Abs(_self.GetAngleTo(_selfBase)) < Math.PI / 2 &&
            //                       _self.GetDistanceTo(_selfBase) > _self.CastRange * 1.5;

            var minBaseDist = double.MaxValue;
            double resX = 0, resY = 0;

            if ((borderDist + radius > nearestTarget.Y) &&
                (!isTop && !isLeft && !isRight && !isBottom))
            {
                var destY = borderDist;
                var desc = nearestTarget.X * nearestTarget.X - (nearestTarget.X * nearestTarget.X +
                                                              Math.Pow(destY - nearestTarget.Y, 2) - radius * radius);
                var destX1 = nearestTarget.X + Math.Sqrt(desc);
                var destX2 = nearestTarget.X - Math.Sqrt(desc);

                var dist1 = distCompareUnit.GetDistanceTo(destX1, destY);
                if (destX1 > 0 && dist1 < minBaseDist)
                {
                    minBaseDist = dist1;
                    resX = destX1;
                    resY = destY;
                }

                var dist2 = distCompareUnit.GetDistanceTo(destX2, destY);
                if (destX2 > 0 && dist2 < minBaseDist)
                {
                    minBaseDist = dist2;
                    resX = destX2;
                    resY = destY;
                }
            }
            if ((borderDist + radius > nearestTarget.X) &&
                (!isTop && !isLeft && !isRight && !isBottom))
            {
                var destX = borderDist;
                var desc = nearestTarget.Y * nearestTarget.Y - (nearestTarget.Y * nearestTarget.Y +
                                                              Math.Pow(destX - nearestTarget.X, 2) - radius * radius);
                var destY1 = nearestTarget.Y + Math.Sqrt(desc);
                var destY2 = nearestTarget.Y - Math.Sqrt(desc);

                var dist1 = distCompareUnit.GetDistanceTo(destX, destY1);
                if (destY1 > 0 && dist1 < minBaseDist)
                {
                    minBaseDist = dist1;
                    resX = destX;
                    resY = destY1;
                }

                var dist2 = distCompareUnit.GetDistanceTo(destX, destY2);
                if (destY2 > 0 && dist2 < minBaseDist)
                {
                    minBaseDist = dist2;
                    resX = destX;
                    resY = destY2;
                }
            }
            if ((_world.Height - borderDist - radius < nearestTarget.Y) &&
                (!isTop && !isLeft && !isRight && !isBottom))
            {
                var destY = _world.Height - borderDist;
                var desc = nearestTarget.X * nearestTarget.X - (nearestTarget.X * nearestTarget.X +
                                                              Math.Pow(destY - nearestTarget.Y, 2) -
                                                              radius * radius);
                var destX1 = nearestTarget.X + Math.Sqrt(desc);
                var destX2 = nearestTarget.X - Math.Sqrt(desc);

                var dist1 = distCompareUnit.GetDistanceTo(destX1, destY);
                if (destX1 > 0 && dist1 < minBaseDist)
                {
                    minBaseDist = dist1;
                    resX = destX1;
                    resY = destY;
                }

                var dist2 = distCompareUnit.GetDistanceTo(destX2, destY);
                if (destX2 > 0 && dist2 < minBaseDist)
                {
                    minBaseDist = dist2;
                    resX = destX2;
                    resY = destY;
                }

            }
            if ((_world.Width - borderDist - radius < nearestTarget.X) && (!isTop &&
                                                                                                !isLeft && !isRight &&
                                                                                                !isBottom))
            {
                var destX = _world.Width - borderDist;
                var desc = nearestTarget.Y * nearestTarget.Y - (nearestTarget.Y * nearestTarget.Y +
                                                              Math.Pow(destX - nearestTarget.X, 2) -
                                                              radius * radius);
                var destY1 = nearestTarget.Y + Math.Sqrt(desc);
                var destY2 = nearestTarget.Y - Math.Sqrt(desc);

                var dist1 = distCompareUnit.GetDistanceTo(destX, destY1);
                if (destY1 > 0 && dist1 < minBaseDist)
                {
                    minBaseDist = dist1;
                    resX = destX;
                    resY = destY1;
                }

                var dist2 = distCompareUnit.GetDistanceTo(destX, destY2);
                if (destY2 > 0 && dist2 < minBaseDist)
                {
                    minBaseDist = dist2;
                    resX = destX;
                    resY = destY2;
                }
            }

            if (minBaseDist < double.MaxValue)
            {
                return new Point2D(resX, resY);
            }
            return null;
        }

        private PointPosition GetPointPosition(Vector v, double pointX,
            double pointY)
        {
            var res = (pointY - v.P1.Y) * (v.P2.X - v.P1.X) - (pointX - v.P1.X) * (v.P2.Y - v.P1.Y);
            if (res == 0) return PointPosition.OnLine;
            if (res < 0) return PointPosition.Left;
            return PointPosition.Right;
        }

        private double GetVectorsAngle(Vector a, Vector b)
        {
            var cos = GetScalarVectorMult(a, b) / a.Length / b.Length;
            if (cos < -1) cos = -1;
            if (cos > 1) cos = 1;
            var angle = Math.Abs(Math.Acos(cos));


            var vectMult = a.X * b.Y - b.X * a.Y;
            if (vectMult < 0) angle *= -1;

            return angle;
        }

        private double GetCriticalGoBackDistance(LivingUnit target)
        {

            var dist = _self.CastRange;
            if (target != null)
            {
                var hasNearWizards =
                    _world.Wizards.Any(
                        w => w.Faction != _self.Faction && _self.GetDistanceTo(w) < _criticalGoBackDistance);

                var buildings = _world.Buildings.Where(b => b.Faction != _self.Faction).ToList();
                buildings.AddRange(_anemyBuildings.Where((t, i) => _IsAnemyBuildingAlive[i]));

                var hasNearBuilding =
                    buildings.Any(b => _self.GetDistanceTo(b) < _criticalGoBackDistance);


                if (hasNearWizards || hasNearBuilding)
                {
                    dist = _self.CastRange * 1.5;
                }
            }
            return dist;
        }


        private void GoBack()
        {
            //_move.Speed = -_game.WizardBackwardSpeed;
            //_move.StrafeSpeed = 0;

            var beforePrevWaypoint = getBeforePreviousWaypoint();
            _thisTickResPoint = beforePrevWaypoint;
            goTo(beforePrevWaypoint, _self.Radius * 2, 0d, false);
        }

        private int GetSquareI(double x)
        {
            var res = (int)((x - _startX) / _squareSize);
            if (res < 0) return 0;
            if (res > _n - 1) return _n - 1;
            return res;
        }

        private int GetSquareJ(double y)
        {
            var res = (int)((y - _startY) / _squareSize);
            if (res < 0) return 0;
            if (res > _m - 1) return _m - 1;
            return res;
        }

        private int GetStaticSquareI(double x)
        {
            var res = (int)((x - _staticStartX) / _staticSquareSize);
            if (res < 0) return 0;
            if (res > _staticN - 1) return _staticN - 1;
            return res;
        }

        private int GetStaticSquareJ(double y)
        {
            var res = (int)((y - _staticStartY) / _staticSquareSize);
            if (res < 0) return 0;
            if (res > _staticM - 1) return _staticM - 1;
            return res;
        }

        private LivingUnit GetObstacle(double speedX, double speedY)
        {
            var time = 50;

            var leftX = _self.X + _self.Radius * Math.Sin(_self.Angle);
            var leftY = _self.Y - _self.Radius * Math.Cos(_self.Angle);

            var rightX = _self.X - _self.Radius * Math.Sin(_self.Angle);
            var rightY = _self.Y + _self.Radius * Math.Cos(_self.Angle);

            var topX = _self.X + _self.Radius * Math.Cos(_self.Angle);
            var topY = _self.Y + _self.Radius * Math.Sin(_self.Angle);

            var bottomX = _self.X - _self.Radius * Math.Cos(_self.Angle);
            var bottomY = _self.Y - _self.Radius * Math.Sin(_self.Angle);

            var nextLeftX = leftX + speedX * time;
            var nextLeftY = leftY + speedY * time;

            var nextRightX = rightX + speedX * time;
            var nextRightY = rightY + speedY * time;

            var nextTopX = topX + speedX * time;
            var nextTopY = topY + speedY * time;

            var nextBottomX = bottomX + speedX * time;
            var nextBottomY = bottomY + speedY * time;

            var targets = new List<LivingUnit>();
            targets.AddRange(_world.Buildings);
            targets.AddRange(_world.Wizards);
            targets.AddRange(_world.Minions);
            targets.AddRange(_world.Trees);

            foreach (
                var target in
                targets.Where(x => x.Id != _self.Id &&
                    (x.Faction == _self.Faction || x.Faction == Faction.Neutral || x.Faction == Faction.Other)))
            {
                var isCross = IsCross(target.X, target.Y, target.Radius, leftX, leftY, nextLeftX,
                                  nextLeftY) ||
                              IsCross(target.X, target.Y, target.Radius, rightX, rightY, nextRightX,
                                  nextRightY) ||
                              IsCross(target.X, target.Y, target.Radius, topX, topY, nextTopX,
                                  nextTopY) ||
                              IsCross(target.X, target.Y, target.Radius, bottomX, bottomY, nextBottomX,
                                  nextBottomY);
                if (isCross) return target;
            }

            return null;
        }

        /// <summary>
        /// ����� ������� �. ������������ ���. (x-x0)^2+(y-y0)^2=R^2 � ��. y=kx+b
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="r"></param>
        /// <param name="k"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private IList<Point2D> GetCrossPoints(double x0, double y0, double r, double k, double b)
        {
            var res = new List<Point2D>();
            var d1 = Math.Pow(x0 - k * b + k * y0, 2) - (1 + k * k) * (x0 * x0 + b * b - 2 * b * y0 + y0 * y0 - r * r);
            if (d1 >= 0)
            {
                var x1 = (x0 - k * b + k * y0 - Math.Sqrt(d1)) / (1 + k * k);
                var x2 = (x0 - k * b + k * y0 + Math.Sqrt(d1)) / (1 + k * k);
                var y1 = k * x1 + b;
                var y2 = k * x2 + b;
                res.Add(new Point2D(x1, y1));
                res.Add(new Point2D(x2, y2));
            }
            return res;
        }

        private IList<Point2D> GetCrossPoints(double x0, double y0, double r, double x1, double y1, double x2, double y2)
        {
            if (x2 == x1)
            {
                //TODO: ��������!
                return new List<Point2D>();
            }

            var k = (y2 - y1) / (x2 - x1);
            var b = y2 - x2 * (y2 - y1) / (x2 - x1);
            return GetCrossPoints(x0, y0, r, k, b);

        }


        private bool IsCross(double x, double y, double r, double x01, double y01, double x02, double y02)
        {
            var rr = r * r;

            //�������� �� ���������� ������ �� ������ ������� � �����
            if ((x01 - x) * (x01 - x) + (y01 - y) * (y01 - y) <= rr) return true;
            if ((x02 - x) * (x02 - x) + (y02 - y) * (y02 - y) <= rr) return true;

            //axis-aligned
            //if (x01-x02==0)
            if (x01 == x02)
            {
                if ((y01 < y && y02 > y || y01 > y && y02 < y) && Math.Abs(x01 - x) <= r) return true;
                return false;
            }
            //if (y01-y02==0)
            if (y01 == y02)
            {
                if ((x01 < x && x02 > x || x01 > x && x02 < x) && Math.Abs(y01 - y) <= r) return true;
                return false;
            }

            //������� ����� (xp,yp) ����������� �������������� �� ������ ����� � �����,
            //������� ����������� �������.
            var a = (y01 - y02) / (x01 - x02);
            var b = y01 - a * x01;
            var xp = (y - b + x / a) / (a + 1 / a);
            var yp = a * xp + b;

            //����������� �������?
            if (x01 < xp && x02 > xp || x02 < xp && x01 > xp)
                //��������� ������ �����?
                if ((xp - x) * (xp - x) + (yp - y) * (yp - y) <= rr) return true;

            return false;
        }


        private LivingUnit GetNearestStaffRangeTarget()
        {
            var targets = new List<LivingUnit>();
            targets.AddRange(_world.Buildings);
            targets.AddRange(_world.Wizards);
            targets.AddRange(_world.Minions);

            LivingUnit nearestTarget = null;
            var minDist = double.MaxValue;
            var minAngle = double.MaxValue;

            foreach (var target in targets)
            {
                if (target.Faction == _self.Faction)
                {
                    continue;
                }


                if (IsCalmNeutralMinion(target)) continue;

                //var angle = _self.GetAngleTo(target);
                //if (Math.Abs(angle) > _game.StaffSector / 2.0D) continue;

                var distance = _self.GetDistanceTo(target) - target.Radius;
                if (distance > _game.StaffRange) continue;


                if (_self.GetDistanceTo(target) <= minDist)
                {
                    if (_self.GetDistanceTo(target) < minDist || Math.Abs(_self.GetAngleTo(target)) < minAngle)
                    {
                        nearestTarget = target;
                        minDist = _self.GetDistanceTo(target);
                        minAngle = Math.Abs(_self.GetAngleTo(target));
                    }
                }
            }

            return nearestTarget;
        }

        private LivingUnit GetNearestTree()
        {
            var targets = new List<LivingUnit>();
            targets.AddRange(_world.Trees);

            LivingUnit nearestTarget = null;
            var minDist = double.MaxValue;
            var minAngle = double.MaxValue;

            foreach (var target in targets)
            {

                var distance = _self.GetDistanceTo(target) - target.Radius;
                if (distance > _game.StaffRange) continue;

                if (_self.GetDistanceTo(target) <= minDist)
                {
                    if (_self.GetDistanceTo(target) < minDist || Math.Abs(_self.GetAngleTo(target)) < minAngle)
                    {
                        nearestTarget = target;
                        minDist = _self.GetDistanceTo(target);
                        minAngle = Math.Abs(_self.GetAngleTo(target));
                    }
                }
            }

            return nearestTarget;
        }

        private LaneType GetNewLaneType()
        {
            var hasTopTowers = GetAliveAnemyTowers(LaneType.Top).Any();
            var hasBottomTowers = GetAliveAnemyTowers(LaneType.Bottom).Any();
            var hasMidTowers = GetAliveAnemyTowers(LaneType.Middle).Any();
            if (_line == LaneType.Top && !hasTopTowers)
            {
                if (hasMidTowers) return LaneType.Middle;
                if (hasBottomTowers) return LaneType.Bottom;
                return LaneType.Top;
            }
            if (_line == LaneType.Bottom && !hasBottomTowers)
            {
                if (hasMidTowers) return LaneType.Middle;
                if (hasTopTowers) return LaneType.Top;
                return LaneType.Bottom;
            }
            if (_line == LaneType.Middle && !hasMidTowers)
            {
                if (hasTopTowers) return LaneType.Top;
                if (hasBottomTowers) return LaneType.Bottom;
                return LaneType.Middle;
            }
            return _line;
        }

        private double GetCorrectAngle()
        {
            var angle = _self.Angle;
            while (angle < 0 || angle >= Math.PI * 2)
            {
                if (angle < 0) angle += Math.PI * 2;
                if (angle >= Math.PI * 2) angle -= Math.PI * 2;
            }
            return angle;
        }

        private double GetDistanceToLine(double a, double b, double c, double x, double y)
        {
            return Math.Abs(a * x + b * y + c) / Math.Sqrt(a * a + b * b);
        }

        private CloseBorder GetCloseBorder()
        {
            //var dist = GetDistanceToLine(1, 1, -_world.Width + 300, _self.X, _self.Y);
            //if (dist < BORDER_RADIUS * 2) return CloseBorder.Middle;

            var angle = GetCorrectAngle();
            var isVertical = angle < Math.PI / 4 || angle > 3 * Math.PI / 4 && angle < 5 * Math.PI / 4 ||
                             angle > 7 * Math.PI / 4;

            var isRightClose = _self.X >= _world.Width - BORDER_RADIUS * 2;
            var isBottomClose = _self.Y >= _world.Height - BORDER_RADIUS * 2;

            var isLeftClose = _self.X <= BORDER_RADIUS * 2;
            var isTopClose = _self.Y <= BORDER_RADIUS * 2;


            if (isRightClose && !isBottomClose) return CloseBorder.Right;
            if (!isRightClose && isBottomClose) return CloseBorder.Bottom;
            if (isRightClose && isBottomClose)
            {
                return isVertical ? CloseBorder.Right : CloseBorder.Bottom;
            }

            if (isLeftClose && !isTopClose) return CloseBorder.Left;
            if (!isLeftClose && isTopClose) return CloseBorder.Top;
            if (isLeftClose && isTopClose)
            {
                return isVertical ? CloseBorder.Left : CloseBorder.Top;
            }

            return CloseBorder.None;


            //if (_self.X <= BORDER_RADIUS * 2 && isVertical) return CloseBorder.Left;
            //if (_self.X >= _world.Width - BORDER_RADIUS * 2 && isVertical) return CloseBorder.Right;
            //if (_self.Y <= BORDER_RADIUS * 2 && !isVertical) return CloseBorder.Top;
            //if (_self.Y >= _world.Height - BORDER_RADIUS * 2 && !isVertical) return CloseBorder.Bottom;

            //return CloseBorder.None;
        }



        //private bool HasFightingFriendBefore(Wizard self, World world, LivingUnit target)
        //{
        //    if (target == null) return false;

        //    List<LivingUnit> friends = new List<LivingUnit>();
        //    friends.AddRange(world.Wizards.Where(x => x.Faction == self.Faction));
        //    friends.AddRange(world.Minions.Where(x => x.Faction == self.Faction));
        //    friends.AddRange(world.Buildings.Where(x => x.Faction == self.Faction));

        //    foreach (var friend in friends)
        //    {
        //        var addDist = friend is Wizard ? 0d : self.CastRange / 2;

        //        if (self.GetDistanceTo(target) < self.CastRange * 2 && 
        //            line == LineType.Top && (friend.X < 600 && friend.Y < self.Y - addDist
        //            || friend.Y < 600 && friend.X > self.X + addDist))
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        //private bool HasFriendBefore(Wizard self, World world)
        //{
        //    return GetFaarestBeforeFriend(self, world) != null;
        //}

        private IList<Building> GetAliveAnemyTowers(LaneType laneType)
        {
            var result = new List<Building>();
            for (int i = 0; i < _anemyBuildings.Count; ++i)
            {
                if (!_IsAnemyBuildingAlive[i] || _anemyBuildings[i].Type != BuildingType.GuardianTower) continue;
                if (laneType == LaneType.Top && _anemyBuildings[i].Y <= 400) result.Add(_anemyBuildings[i]);
                if (laneType == LaneType.Bottom && _anemyBuildings[i].X >= _world.Width - 400) result.Add(_anemyBuildings[i]);
                if (laneType == LaneType.Middle && _anemyBuildings[i].X > 400 && _anemyBuildings[i].X < _world.Width - 400 &&
                    _anemyBuildings[i].Y > 400 && _anemyBuildings[i].Y < _world.Width - 400) result.Add(_anemyBuildings[i]);
            }
            return result;
        }

        private bool IsOkToBurnBase()
        {
            for (int i = 0; i < _anemyBuildings.Count; ++i)
            {
                if (_anemyBuildings[i].Type == BuildingType.GuardianTower && _IsAnemyBuildingAlive[i])
                {
                    return false;
                }
            }
            return true;
        }

        /**
     * ������������� ���������.
     * <p>
     * ��� ���� ����� ������ ����� ������������ �����������, ������ � ������ ������ �� ����� ���������������� ���������
     * ��������� ����� ���������, ���������� �� ���������� ����.
     */
        private void initializeStrategy(Wizard self, Game game)
        {
            if (_random == null)
            {
                _random = new Random(DateTime.Now.Millisecond);
                _bulletStartDatas = new Dictionary<long, BulletStartData>();

                var radiusSum = _self.Radius + _game.BonusRadius;
                _bonus0TopPoint = new Point2D(
                    _bonusPoints[0].X - radiusSum * Math.Cos(Math.PI / 4),
                    _bonusPoints[0].Y - radiusSum * Math.Cos(Math.PI / 4));
                _bonus0BottomPoint = new Point2D(
                    _bonusPoints[0].X + radiusSum * Math.Cos(Math.PI / 4),
                    _bonusPoints[0].Y + radiusSum * Math.Cos(Math.PI / 4));

                _bonus1TopPoint = new Point2D(
                  _bonusPoints[1].X - radiusSum * Math.Cos(Math.PI / 4),
                  _bonusPoints[1].Y - radiusSum * Math.Cos(Math.PI / 4));
                _bonus1BottomPoint = new Point2D(
                    _bonusPoints[1].X + radiusSum * Math.Cos(Math.PI / 4),
                    _bonusPoints[1].Y + radiusSum * Math.Cos(Math.PI / 4));

                _needChangeLine = false;

                _criticalNearestTargetDistance = _game.GuardianTowerAttackRange + _self.Radius * 2;

                _myBuildings = new List<Building>();
                _anemyBuildings = new List<Building>();
                _IsMyBuildingAlive = new List<bool>();
                _IsAnemyBuildingAlive = new List<bool>();

                var selfBuilding = _world.Buildings.Where(x => x.Faction == _self.Faction);
                foreach (var building in selfBuilding)
                {
                    _myBuildings.Add(building);
                    _anemyBuildings.Add(
                        new Building(
                            0,
                            _world.Width - building.X,
                            _world.Height - building.Y,
                            building.SpeedX,
                            building.SpeedY,
                            building.Angle,
                            _self.Faction == Faction.Academy ? Faction.Renegades : Faction.Academy,
                            building.Radius,
                            building.Life,
                            building.MaxLife,
                            building.Statuses,
                            building.Type,
                            building.VisionRange,
                            building.AttackRange,
                            building.Damage,
                            building.CooldownTicks,
                            building.RemainingActionCooldownTicks));
                    _IsMyBuildingAlive.Add(true);
                    _IsAnemyBuildingAlive.Add(true);
                }

                double mapSize = game.MapSize;

                var topTowers = GetAliveAnemyTowers(LaneType.Top);
                var bottomTowers = GetAliveAnemyTowers(LaneType.Bottom);
                var midTowers = GetAliveAnemyTowers(LaneType.Middle);

                _waypointsByLine.Add(LaneType.Middle, new Point2D[]{
                    new Point2D(100.0D, mapSize - 100.0D),
                    _random.Next() % 2 == 0
                            ? new Point2D(600.0D, mapSize - 200.0D)
                            : new Point2D(200.0D, mapSize - 600.0D),
                    new Point2D(800.0D, mapSize - 800.0D),
                    new Point2D(1600.0D, mapSize - 1600),
                    new Point2D((midTowers[0].X + midTowers[1].X) / 2, (midTowers[0].Y + midTowers[1].Y) / 2),
                    new Point2D(mapSize - 600.0D, 600.0D)
            });

                _waypointsByLine.Add(LaneType.Top, new Point2D[]{
                    new Point2D(100.0D, mapSize - 100.0D),
                    new Point2D(100.0D, mapSize - 400.0D),
                    new Point2D(200.0D, mapSize - 800.0D),
                    new Point2D(200.0D, mapSize * 0.75D),
                    new Point2D(200.0D, mapSize * 0.5D),
                    new Point2D(200.0D, mapSize * 0.25D),
                    new Point2D(300.0D, 300.0D),
                    new Point2D(mapSize * 0.25D, 200.0D),
                    new Point2D(mapSize * 0.5D, 200.0D),
                    new Point2D(mapSize * 0.75D, 200.0D),
                    new Point2D(mapSize - 200.0D, 200.0D)
            });

                _waypointsByLine.Add(LaneType.Bottom, new Point2D[]{
                    new Point2D(100.0D, mapSize - 100.0D),
                    new Point2D(400.0D, mapSize - 100.0D),
                    new Point2D(800.0D, mapSize - 200.0D),
                    new Point2D(mapSize * 0.25D, mapSize - 200.0D),
                    new Point2D(mapSize * 0.5D, mapSize - 200.0D),
                    new Point2D(mapSize * 0.75D, mapSize - 200.0D),
                    new Point2D(mapSize - 300.0D, mapSize - 300.0D),
                    new Point2D(mapSize - 200.0D, mapSize * 0.75D),
                    new Point2D(mapSize - 200.0D, mapSize * 0.5D),
                    new Point2D(mapSize - 200.0D, mapSize * 0.25D),
                    new Point2D(mapSize - 200.0D, 200.0D)
            });

                _line = LaneType.Middle;

                //                    switch ((int) self.Id)
                //                   {
                //                        case 1:
                //                        case 2:
                //                        case 6:
                //                        case 7:
                //                            _line = LaneType.Top;
                //                            break;
                //                        case 3:
                //                        case 8:
                //
                //                            _line = LaneType.Middle;
                //                            break;
                //                        case 4:
                //                        case 5:
                //                        case 9:
                //                        case 10:
                //                            _line = LaneType.Bottom;
                //                            break;
                //
                //                    }



                // ���� ��������� ������� �� �������������, ��� �������� ���� �������� ����� ����������� �� ��������
                // ��������� �� ��������� �������� �����. ������ �������� ����� ����� ���������, ������ �� ������
                // �������� ���� ��������, ���� ������ �������� ���������� �������� �����.

                /*Point2D lastWaypoint = waypoints[waypoints.length - 1];

                Preconditions.checkState(ArrayUtils.isSorted(waypoints, (waypointA, waypointB) -> Double.compare(
                        waypointB.getDistanceTo(lastWaypoint), waypointA.getDistanceTo(lastWaypoint)
                )));*/
            }
        }


        /**
    * ��������� ��� ������� ������ � ����� ������ ��� ��������� ������� � ���.
    */
        private void initializeTick(Wizard self, World world, Game game, Move move)
        {
            _self = self;
            _world = world;
            _game = game;
            _move = move;
        }

        /**
  * ������ ����� ������������, ��� ��� �������� ����� �� ����� ����������� �� ���������� ��������� �� ���������
  * �������� �����. ��������� �� �� �������, ������� ������ ���������� �����, ������� ��������� ����� � ���������
  * ����� �� �����, ��� ���������. ��� � ����� ��������� �������� ������.
  * <p>
  * ������������� ���������, �� ��������� �� ��������� ���������� ������ � �����-���� �� �������� �����. ���� ���
  * ���, �� �� ����� ���������� ��������� �������� �����.
  */
        private Point2D getNextWaypoint()
        {
            int lastWaypointIndex = _waypointsByLine[_line].Length - 1;
            Point2D lastWaypoint = _waypointsByLine[_line][lastWaypointIndex];

            //var thisLineAnemyUnits = new List<LivingUnit>();
            //foreach (var w in _world.Wizards.Where(x => x.Faction != Faction.Neutral && x.Faction != _self.Faction))
            //{
            //    if (IsOnLine(w)) thisLineAnemyUnits.Add(w);
            //}
            //foreach (var m in _world.Minions.Where(x => x.Faction != Faction.Neutral && x.Faction != _self.Faction))
            //{
            //    if (IsOnLine(m)) thisLineAnemyUnits.Add(m);
            //}

            for (int waypointIndex = 0; waypointIndex < lastWaypointIndex; ++waypointIndex)
            {


                Point2D waypoint = _waypointsByLine[_line][waypointIndex];

                //if (thisLineAnemyUnits.Any())
                //{
                //    if (thisLineAnemyUnits.Any(u => lastWaypoint.getDistanceTo(waypoint) < lastWaypoint.getDistanceTo(u)))
                //    {
                //        return waypointIndex > 0 ? _waypointsByLine[_line][waypointIndex] : _waypointsByLine[_line][0];
                //    }
                //}

                if (waypoint.getDistanceTo(_self) <= WAYPOINT_RADIUS)
                {
                    return _waypointsByLine[_line][waypointIndex + 1];
                }

                if (lastWaypoint.getDistanceTo(waypoint) < lastWaypoint.getDistanceTo(_self))
                {
                    return waypoint;
                }
            }

            return lastWaypoint;
        }

        /**
         * �������� ������� ������ ��������� ��������� �������� ������ {@code getNextWaypoint}, ���� ����������� ������
         * {@code waypoints}.
         */
        private Point2D getPreviousWaypoint()
        {
            Point2D firstWaypoint = _waypointsByLine[_line][0];

            for (int waypointIndex = _waypointsByLine[_line].Length - 1; waypointIndex > 0; --waypointIndex)
            {
                Point2D waypoint = _waypointsByLine[_line][waypointIndex];

                if (waypoint.getDistanceTo(_self) <= 2 * WAYPOINT_RADIUS)
                {
                    return _waypointsByLine[_line][waypointIndex - 1];
                }

                if (firstWaypoint.getDistanceTo(waypoint) < firstWaypoint.getDistanceTo(_self))
                {
                    return waypoint;
                }
            }

            return firstWaypoint;
        }


        private Point2D getBeforePreviousWaypoint()
        {
            Point2D firstWaypoint = _waypointsByLine[_line][0];

            for (int waypointIndex = _waypointsByLine[_line].Length - 1; waypointIndex > 0; --waypointIndex)
            {
                Point2D waypoint = _waypointsByLine[_line][waypointIndex];

                if (waypoint.getDistanceTo(_self) <= WAYPOINT_RADIUS)
                {
                    return _waypointsByLine[_line][waypointIndex - 1];
                }

                if (firstWaypoint.getDistanceTo(waypoint) < firstWaypoint.getDistanceTo(_self))
                {
                    return _waypointsByLine[_line][waypointIndex - 1];
                }
            }

            return firstWaypoint;
        }


        private double GetPathWeight(IList<Point> path)
        {
            var weight = 0d;
            for (int i = 0; i < path.Count - 1; ++i)
            {
                weight += path[i].GetCost(path[i + 1], _self, _game);
            }
            return weight;
        }

        private bool CanMakePath(IList<Point> path)
        {
            var units = new List<LivingUnit>();
            units.AddRange(_world.Buildings);
            units.AddRange(_world.Minions);
            units.AddRange(_world.Wizards.Where(x => !x.IsMe));
            units.AddRange(_world.Trees);

            if (path.Count < 2) return true;

            //foreach (var unit in units)
            //{
            //    if (Square.Intersect(_self.X, _self.Y, (path[1] as Square).X + (path[1] as Square).Side/2,
            //        (path[1] as Square).Y + (path[1] as Square).Side/2, unit.X, unit.Y, _self.Radius, unit.Radius))
            //        return false;
            //}

            for (int i = 0; i < path.Count - 1; ++i)
            {
                var p0X = (path[i] as Square).X + (path[i] as Square).Side / 2;
                var p0Y = (path[i] as Square).Y + (path[i] as Square).Side / 2;
                var p1X = (path[i + 1] as Square).X + (path[i + 1] as Square).Side / 2;
                var p1Y = (path[i + 1] as Square).Y + (path[i + 1] as Square).Side / 2;
                foreach (var unit in units)
                {
                    if (unit is Tree) continue;

                    if (Square.Intersect(p0X, p0Y, p1X, p1Y, unit.X, unit.Y, _self.Radius, unit.Radius))
                        return false;
                }
            }
            return true;
        }

        private IList<Point> GetCorrectPath(IList<Point> path)
        {
            var resPath = path;
            if (_lastTickResPoint != null && _lastTickPath != null &&
                Math.Abs(_thisTickResPoint.X - _lastTickResPoint.X) < _game.WizardForwardSpeed &&
                Math.Abs(_thisTickResPoint.Y - _lastTickResPoint.Y) < _game.WizardForwardSpeed &&
                GetPathWeight(path) > GetPathWeight(_lastTickPath)) //_game.WizardForwardSpeed, ����� �������� ���������� ����
            {
                if (CanMakePath(_lastTickPath))
                {

                    if (_lastTickPath.Count > 1)
                    {
                        var path01Dist = (_lastTickPath[1] as Square).GetHeuristicCost(_lastTickPath[0]);
                        var path0Dist = _self.GetDistanceTo(
                            (_lastTickPath[0] as Square).X + _squareSize / 2,
                            (_lastTickPath[0] as Square).Y + _squareSize / 2);
                        if (path0Dist + TOLERANCE >= path01Dist) _lastTickPath.RemoveAt(0);
                    }
                    resPath = _lastTickPath;
                }
            }
            return resPath;
        }

        private bool IsCalmNeutralMinion(LivingUnit unit)
        {
            var minion = unit as Minion;
            if (minion == null) return false;
            if (minion.Faction != Faction.Neutral) return false;
            return minion.Life == minion.MaxLife && minion.SpeedX == 0 && minion.SpeedY == 0 &&
                   minion.RemainingActionCooldownTicks == 0;
        }

        private double GetSquareCenterX(Point point)
        {
            var square = point as Square;
            return square.X + square.Side / 2d;
        }
        private double GetSquareCenterY(Point point)
        {
            var square = point as Square;
            return square.Y + square.Side / 2d;
        }

        private LivingUnit GetWoodCutTree(Square path0, Square path1)
        {
            var trees = new List<LivingUnit>();
            if (path0.Units != null) trees.AddRange(path0.Units.Where(x => x is Tree));
            if (path1.Units != null) trees.AddRange(path1.Units.Where(x => x is Tree));
            var minDist = double.MaxValue;
            LivingUnit nearestTree = null;
            foreach (var tree in trees)
            {
                if (Square.Intersect(
                    GetSquareCenterX(path0),
                    GetSquareCenterY(path0),
                    GetSquareCenterX(path1),
                    GetSquareCenterY(path1),
                    tree.X,
                    tree.Y,
                    _self.Radius,
                    tree.Radius))
                {
                    var dist = tree.GetDistanceTo(path0.X, path0.Y);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestTree = tree;
                    }
                }
            }
            return nearestTree;
        }

        private void UpdateLastTickPathTrees()
        {
            var trees = _world.Trees;
            if (_lastTickPath != null)
            {
                foreach (Square s in _lastTickPath)
                {
                    if (s.Units == null) continue;
                    s.Units.RemoveWhere(
                        unit => !trees.Any(t => Math.Abs(t.X - unit.X) < TOLERANCE && Math.Abs(t.Y - unit.Y) < TOLERANCE));
                }
            }
        }

        private bool goTo(Point2D point, double relaxCoeff, double strightRelaxCoeff, bool needTurn, IList<Point> path = null)
        {
            //if (!_self.IsMaster && _world.TickIndex < CHECK_MASTER_TIME) return false;

            SpeedContainer speedContainer = null;
            var goStraightPoint = GetGoStraightPoint(point.X, point.Y, strightRelaxCoeff);
            if (goStraightPoint != null)
            {
                speedContainer = GetSpeedContainer(goStraightPoint.X, goStraightPoint.Y);
                var isInPoint = Math.Abs(_self.X - point.X) < TOLERANCE &&
                                Math.Abs(_self.Y - point.Y) < TOLERANCE;
                if (needTurn && !isInPoint)
                {
                    _move.Turn = _self.GetAngleTo(point.X, point.Y);
                }

                _move.Speed = speedContainer.Speed;
                _move.StrafeSpeed = speedContainer.StrafeSpeed;
                _lastTickPath = null;
                _lastTickResPoint = null;
                return false;
            }

            if (path == null)
            {
                var goalI = GetSquareI(point.X);
                var goalJ = GetSquareJ(point.Y);
                path = Calculator.GetPath(_startSquare, _table[goalI, goalJ], _squares, _self, _game, relaxCoeff);
                path = GetCorrectPath(path);
            }

            //foreach (var p in path)
            //{
            //    Debug.circle((p as Square).X + _squareSize / 2, (p as Square).Y + _squareSize / 2, _squareSize / 2, 150);
            //}


            double resX;
            double resY;

            //var aStarDeltaToChangePoint = _self.Radius*Math.Sqrt(17)/2;
            //var aStarDeltaToChangePointDiagonal = _self.Radius*Math.Sqrt(41)/2;
            //var isDiagonalLine = path.Count > 1
            //    ? (path[1] as Square).X != (path[0] as Square).X && (path[1] as Square).Y != (path[0] as Square).Y
            //    : false;
            //var distToPath1 = path.Count > 1
            //    ? _self.GetDistanceTo((path[1] as Square).X + _squareSize/2, (path[1] as Square).Y + _squareSize/2)
            //    : 0d;

            //if (path.Count > 1 &&
            //    (isDiagonalLine && distToPath1 >= aStarDeltaToChangePointDiagonal ||
            //     !isDiagonalLine && distToPath1 >= aStarDeltaToChangePoint))
            //{
            //    resX = (path[0] as Square).X + _squareSize/2;
            //    resY = (path[0] as Square).Y + _squareSize/2;
            //}
            if (path.Count <= 1)
            {
                resX = point.X;
                resY = point.Y;
            }
            //else if (checkIsOkToGoBack && !IsOkToGoBack(path))
            //// ���� ASTAR ����� � �����, �� ������� ���
            //{
            //    var prevWaypoint = getPreviousWaypoint();
            //    resX = prevWaypoint.X;
            //    resY = prevWaypoint.Y;
            //}
            else
            {
                resX = (path[1] as Square).X + _squareSize / 2;
                resY = (path[1] as Square).Y + _squareSize / 2;
            }

            speedContainer = GetSpeedContainer(resX, resY);


            _move.Speed = speedContainer.Speed;
            _move.StrafeSpeed = speedContainer.StrafeSpeed;

            LivingUnit woodCutTree = null;
            if (path.Count > 1)
            {
                woodCutTree = GetWoodCutTree(path[0] as Square, path[1] as Square);
                if (woodCutTree != null)
                {

                    var angle = _self.GetAngleTo(woodCutTree);
                    //if (Math.Abs(angle) > _game.StaffSector/2)
                    //{
                    _move.Turn = angle;
                    var distance = _self.GetDistanceTo(woodCutTree) - woodCutTree.Radius;
                    //}

                    if (Math.Abs(angle) <= _game.StaffSector / 2.0D)
                    {
                        if (_self.RemainingActionCooldownTicks == 0 && distance <= _game.StaffRange &&
                            _self.RemainingCooldownTicksByAction[(int)ActionType.Staff] == 0)
                        {
                            _move.Action = ActionType.Staff;
                        }
                        else if (_self.RemainingActionCooldownTicks == 0 &&
                                 _self.RemainingCooldownTicksByAction[(int)ActionType.MagicMissile] == 0)
                        {
                            _move.Action = ActionType.MagicMissile;
                        }
                    }

                }
            }

            if (needTurn && woodCutTree == null)
            {
                _move.Turn = _self.GetAngleTo(resX, resY);
            }

            var pointsPath = new List<Point2D>();
            foreach (var p in path)
            {
                pointsPath.Add(
                    new Point2D(
                        (p as Square).X + (p as Square).Side / 2,
                        (p as Square).X + (p as Square).Side / 2));
            }

            _lastTickPath = new List<Point>();
            foreach (Square p in path)
            {
                _lastTickPath.Add(new Square(p.Side, p.X, p.Y, 1d, p.Name, _game));
            }
            _lastTickResPoint = new Point2D(_thisTickResPoint.X, _thisTickResPoint.Y);

            return woodCutTree != null;
        }


        private LivingUnit GetClosestTarget()
        {
            var targets = new List<LivingUnit>();
            targets.AddRange(_world.Buildings);
            targets.AddRange(_world.Wizards);
            targets.AddRange(_world.Minions);

            //for (int i = 0; i < _anemyBuildings.Count; ++i)
            //{
            //    if (_IsAnemyBuildingAlive[i])
            //    {
            //        targets.Add(_anemyBuildings[i]);
            //    }
            //}

            LivingUnit closestTarget = null;
            var minDist = double.MaxValue;

            foreach (var target in targets)
            {
                if (target.Faction == _self.Faction)
                {
                    continue;
                }


                if (IsCalmNeutralMinion(target)) continue;

                var dist = _self.GetDistanceTo(target);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestTarget = target;
                }
            }
            return closestTarget;
        }

        private LivingUnit GetShootingTarget()
        {
            var wizards = _world.Wizards.Where(x => x.Faction != _self.Faction);
            LivingUnit shootingTarget = null;
            //LivingUnit possibleShootingTarget = null;

            var minHp = double.MaxValue;
            //var possibleMinHp = double.MaxValue;

            foreach (var target in wizards)
            {
                if (target.Faction == _self.Faction) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;

                //var canShootWizard = CanShootWizard(_self, target);
                var needRunBack = CanShootWizard(_self, target);

                var life = target.Life;

                if (needRunBack)
                {

                    if (life < minHp)
                    {
                        minHp = life;
                        shootingTarget = target;
                    }
                }
                //else
                //{
                //    if (life < possibleMinHp)
                //    {
                //        possibleMinHp = life;
                //        possibleShootingTarget = target;
                //    }
                //}
            }

            if (shootingTarget != null) return shootingTarget;


            var minDist = double.MaxValue;
            foreach (var target in _world.Buildings)
            {
                if (target.Faction == _self.Faction) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;

                double distance = _self.GetDistanceTo(target);

                if (distance < minDist && target.Life <= target.MaxLife * 0.25)
                {
                    minDist = distance;
                    shootingTarget = target;
                }
            }
            if (shootingTarget != null) return shootingTarget;


            var minions = _world.Minions;
            minHp = double.MaxValue;
            foreach (var target in minions)
            {
                if (target.Faction == _self.Faction) continue;
                if (IsCalmNeutralMinion(target)) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;

                //double distance = _self.GetDistanceTo(target);

                var life = target.Life;
                if (life < minHp)
                {
                    minHp = life;
                    shootingTarget = target;
                }
            }

            if (shootingTarget != null) return shootingTarget;


            minDist = double.MaxValue;
            foreach (var target in _world.Buildings)
            {
                if (target.Faction == _self.Faction) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;

                double distance = _self.GetDistanceTo(target);

                if (distance < minDist)
                {
                    minDist = distance;
                    shootingTarget = target;
                }
            }
            if (shootingTarget != null) return shootingTarget;

            return null;
        }

        private bool CanShoot(LivingUnit unit)
        {
            //TODO
            var angle = _self.GetAngleTo(unit);
            if (Math.Abs(angle) >= _game.StaffSector / 2.0D) return false;

            //if (unit is Wizard)
            //{
            //    return IsOkDistanceToShootWizardWithMissile(_self, unit);
            //}

            return IsOkDistanceToShoot(_self, unit, 0d);
        }

        private bool IsOkDistanceToShoot(LivingUnit source, LivingUnit target, double eps)
        {
            var distance = source.GetDistanceTo(target);
            if (source is Minion && (source as Minion).Type == MinionType.OrcWoodcutter)
            {
                return _game.OrcWoodcutterAttackRange >= distance - target.Radius - _game.MinionSpeed * 3;
            }

            if (source is Building) return (source as Building).AttackRange >= distance;

            var attackRange = 0d;
            if (source is Wizard) attackRange = (source as Wizard).CastRange;
            if (source is Minion && (source as Minion).Type == MinionType.FetishBlowdart)
                attackRange = _game.FetishBlowdartAttackRange;

            return attackRange >= distance - target.Radius + _game.MagicMissileRadius * 1.5 - eps;
        }


        //private bool IsOkDistanceToShootWizardWithMissile(LivingUnit source, LivingUnit target)
        //{
        //    var attackRange = 0d;
        //    if (source is Wizard) attackRange = (source as Wizard).CastRange;
        //    if (source is Building) attackRange = (source as Building).AttackRange;
        //    if (source is Minion && (source as Minion).Type == MinionType.FetishBlowdart)
        //        attackRange = _game.FetishBlowdartAttackRange;

        //    var dist = source.GetDistanceTo(target);

        //    var time = dist / _game.MagicMissileSpeed;

        //    var realDist = dist + time*_game.WizardBackwardSpeed;
        //    return attackRange >= realDist - target.Radius + _game.MagicMissileRadius * 1.5;
        //}

        private bool IsOkDistanceToShootWizardWithFrostBolt(LivingUnit source, Wizard target)
        {
            var attackRange = 0d;
            if (source is Wizard) attackRange = (source as Wizard).CastRange;
            if (source is Building) attackRange = (source as Building).AttackRange;
            if (source is Minion && (source as Minion).Type == MinionType.FetishBlowdart)
                attackRange = _game.FetishBlowdartAttackRange;

            var dist = source.GetDistanceTo(target);
            var time = dist / _game.FrostBoltSpeed;

            var realDist = dist + time * GetWizardMaxBackSpeed(target);
            return attackRange >= realDist - target.Radius + _game.FrostBoltRadius * 1.5;
        }

        /**
         * ������� ��������� ���� ��� �����, ���������� �� � ���� � ������ �������������.
         */
        private
        LivingUnit getNearestTarget()
        {
            var targets = new List<LivingUnit>();
            targets.AddRange(_world.Buildings);
            targets.AddRange(_world.Wizards);
            targets.AddRange(_world.Minions);
            //for (int i = 0; i < _anemyBuildings.Count; ++i)
            //{
            //    if (_IsAnemyBuildingAlive[i])
            //    {
            //        targets.Add(_anemyBuildings[i]);
            //    }
            //}

            LivingUnit nearestTarget = null;
            double nearestTargetDistance = Double.MaxValue;
            var nearestTargetAngle = Double.MaxValue;
            var minLife = double.MaxValue;



            foreach (var target in targets)
            {
                if (target.Faction == _self.Faction)
                {
                    continue;
                }

                if (IsCalmNeutralMinion(target)) continue;


                double distance = _self.GetDistanceTo(target);
                var angle = Math.Abs(_self.GetAngleTo(target));
                var life = target.Life;

                //var hasBeforeFriends = HasBeforeFriends(0d, _self.CastRange / 3);
                var isClose = distance < _self.CastRange;

                if (Math.Abs(angle) < Math.PI / 4 && isClose &&
                    life < minLife)
                {
                    nearestTarget = target;
                    minLife = life;
                }
            }

            if (nearestTarget == null)
            {
                nearestTargetDistance = Double.MaxValue;
                foreach (var target in targets)
                {
                    if (target.Faction == _self.Faction)
                    {
                        continue;
                    }
                    if (IsCalmNeutralMinion(target)) continue;

                    if (!IsOnLine(target)) continue;

                    var building = target as Building;
                    if (building != null && building.Type == BuildingType.FactionBase && !IsOkToBurnBase()) continue;


                    double distance = _self.GetDistanceTo(target);
                    if (distance < nearestTargetDistance)
                    {
                        nearestTarget = target;
                        nearestTargetDistance = distance;
                    }
                }

            }

            return nearestTarget;
        }

        private bool IsOnCrossLine()
        {
            if (_self.X >= _world.Width / 2 - ROW_WIDTH && _self.X <= _world.Width / 2 + ROW_WIDTH &&
                _self.Y >= _world.Height / 2 - ROW_WIDTH && _self.Y <= _world.Height / 2 + ROW_WIDTH)
            {
                return true;
            }

            if (_self.Y >= _world.Height - 2 * ROW_WIDTH && _self.X >= _self.Y)
            {
                return true;
            }

            if (_self.X <= 2 * ROW_WIDTH && _self.Y <= _self.X)
            {
                return true;
            }

            var pos3 =
                GetPointPosition(
                    new Vector()
                    {
                        P1 = new Point2D(0, ROW_WIDTH),
                        P2 = new Point2D(_world.Width - ROW_WIDTH, _world.Height)
                    }, _self.X, _self.Y);

            var pos4 =
                GetPointPosition(
                    new Vector()
                    {
                        P1 = new Point2D(ROW_WIDTH, 0),
                        P2 = new Point2D(_world.Width, _world.Height - ROW_WIDTH)
                    }, _self.X, _self.Y);

            return (pos3 == PointPosition.Left && pos4 == PointPosition.Right);
        }

        private bool IsStrongOnLine(LivingUnit livingUnit, LaneType laneType)
        {
            var isNearToBase = livingUnit.X < 2 * ROW_WIDTH && livingUnit.Y > _world.Height - 2 * ROW_WIDTH;

            if (isNearToBase) return false;

            var isOnTop = livingUnit.Y < ROW_WIDTH || livingUnit.X < ROW_WIDTH ||
                        livingUnit.Y < _world.Width / 2 - livingUnit.X;

            if (laneType == LaneType.Top)
            {
                return isOnTop;
            }

            var isOnBottom = livingUnit.Y > _world.Height - ROW_WIDTH || livingUnit.X > _world.Width - ROW_WIDTH ||
                             livingUnit.Y > 3 * _world.Width / 2 - livingUnit.X;
            if (laneType == LaneType.Bottom)
            {
                return isOnBottom;
            }

            var isOnMainDiagonal = livingUnit.Y >= _world.Width - ROW_WIDTH - livingUnit.X &&
                                   livingUnit.Y <= _world.Width + ROW_WIDTH - livingUnit.X;

            //Mid
            return !isOnTop && !isOnBottom && isOnMainDiagonal;
        }

        private bool IsOnLine(LivingUnit livingUnit)
        {
            var isNearToBase = livingUnit.X < 2 * ROW_WIDTH && livingUnit.Y > _world.Height - 2 * ROW_WIDTH ||
                               livingUnit.Y < 2 * ROW_WIDTH && livingUnit.X > _world.Width - 2 * ROW_WIDTH;
            if (isNearToBase) return true;


            if (_line == LaneType.Top)
            {
                return livingUnit.Y < _world.Width - ROW_WIDTH - livingUnit.X;
            }
            if (_line == LaneType.Bottom)
            {
                return livingUnit.Y > _world.Width + ROW_WIDTH - livingUnit.X;
            }

            //Mid
            return livingUnit.Y >= ROW_WIDTH && livingUnit.X >= ROW_WIDTH &&
                   livingUnit.Y <= _world.Height - ROW_WIDTH && livingUnit.X <= _world.Width - ROW_WIDTH;


        }

        private bool CanKillWizard(Wizard target)
        {
            //var currTargetLife = target.Life;
            //var currSelfLife = _self.Life;
            //var t = 0;

            //while (currTargetLife > 0 && currSelfLife > 0)
            //{
            //    if (CanShootWithFireball())
            //}
            //if (currSelfLife <= 0) return false;
            //if (currTargetLife <= 0) return true;

            return false;
        }

        #region SpeedCalc

        private double GetResultSpeed(Wizard wizard, double defaultSpeed)
        {
            var resultSpeed = defaultSpeed;

            if (wizard.Skills.Any(x => x == SkillType.MovementBonusFactorAura2))
            {
                resultSpeed += defaultSpeed * 0.2;
            }
            else if (wizard.Skills.Any(x => x == SkillType.MovementBonusFactorPassive2))
            {
                resultSpeed += defaultSpeed * 0.15;
            }
            else if (wizard.Skills.Any(x => x == SkillType.MovementBonusFactorAura1))
            {
                resultSpeed += defaultSpeed * 0.1;
            }
            else if (wizard.Skills.Any(x => x == SkillType.MovementBonusFactorPassive1))
            {
                resultSpeed += defaultSpeed * 0.05;
            }

            if (wizard.Statuses.Any(x => x.Type == StatusType.Hastened))
            {
                resultSpeed += defaultSpeed * 0.3;
            }

            var nearWizards =
                _world.Wizards.Where(
                    w =>
                        w.Id != wizard.Id && w.Faction == wizard.Faction &&
                        w.GetDistanceTo(wizard) <= _game.AuraSkillRange);
            SkillType? maxAura = null;
            foreach (var w in nearWizards)
            {
                if (w.Skills.Contains(SkillType.MovementBonusFactorPassive2))
                {
                    maxAura = SkillType.MovementBonusFactorPassive2;
                    break;
                }
                if (w.Skills.Contains(SkillType.MovementBonusFactorPassive1))
                {
                    maxAura = SkillType.MovementBonusFactorPassive1;
                }
            }

            if (maxAura == SkillType.MovementBonusFactorPassive2)
            {
                resultSpeed += defaultSpeed * 0.1;
            }
            else if (maxAura == SkillType.MovementBonusFactorPassive1)
            {
                resultSpeed += defaultSpeed * 0.05;
            }


            return resultSpeed;
        }

        private double GetWizardMaxForwardSpeed(Wizard wizard)
        {
            return GetResultSpeed(wizard, _game.WizardForwardSpeed);
        }

        private double GetWizardMaxBackSpeed(Wizard wizard)
        {
            return GetResultSpeed(wizard, _game.WizardBackwardSpeed);
        }

        private double GetWizardMaxStrafeSpeed(Wizard wizard)
        {
            return GetResultSpeed(wizard, _game.WizardStrafeSpeed);
        }

        private double GetWizardMaxTurn(Wizard wizard)
        {
            var resultTurn = _game.WizardMaxTurnAngle;
            if (wizard.Statuses.Any(x => x.Type == StatusType.Hastened))
            {
                resultTurn += _game.WizardMaxTurnAngle * 0.5;
            }
            return resultTurn;
        }

        #endregion


        private bool CanGoToStaffRangeNew(ref double runBackTime)
        {
            var friends = new List<LivingUnit>();
            friends.AddRange(_world.Wizards.Where(x => x.Faction == _self.Faction));
            friends.AddRange(_world.Minions.Where(x => x.Faction == _self.Faction));
            friends.AddRange(_world.Buildings.Where(x => x.Faction == _self.Faction));

            if (friends.Count < 2) return false;

            var anemies = new List<LivingUnit>();
            anemies.AddRange(_world.Wizards.Where(x => x.Faction != _self.Faction && x.GetDistanceTo(_self) <= _self.CastRange * 1.5));
            anemies.AddRange(_world.Minions.Where(x => x.Faction != _self.Faction && x.GetDistanceTo(_self) <= _self.CastRange * 1.5));


            for (int i = 0; i < _anemyBuildings.Count; ++i)
            {
                if (!_IsAnemyBuildingAlive[i]) continue;
                if (_anemyBuildings[i].GetDistanceTo(_self) <
                    _anemyBuildings[i].AttackRange + 2 * _self.Radius)
                {
                    anemies.Add(_anemyBuildings[i]);
                }
            }



            foreach (var anemy in anemies)
            {
                if (IsCalmNeutralMinion(anemy)) continue;

                //var needRunBack = NeedRunBack(anemy, _self, friends, ref runBackTime);
                var needRunBack = NeedRunBack(anemy, _self, friends, true);
                if (needRunBack) return false;

            }

            return true;
        }

        private SpeedContainer GetSpeedContainer(double x, double y)
        {
            var selfVector = new Vector()
            {
                P1 = new Point2D(_self.X, _self.Y),
                P2 = new Point2D(_self.X + 1 * Math.Cos(_self.Angle), _self.Y + 1 * Math.Sin(_self.Angle))
            };

            var speedVector = new Vector()
            {
                P1 = new Point2D(_self.X, _self.Y),
                P2 = new Point2D(x, y)
            };

            if (Math.Abs(speedVector.X) < TOLERANCE && Math.Abs(speedVector.Y) < TOLERANCE)
            {
                return new SpeedContainer() { Speed = 0, StrafeSpeed = 0 };
            }

            var angle =
                GetVectorsAngle(selfVector, speedVector);

            var forwardSpeed = 1 * Math.Cos(angle);
            var strafeSpeed = 1 * Math.Sin(angle);

            if (Math.Abs(forwardSpeed) < TOLERANCE)
            {
                return new SpeedContainer()
                {
                    Speed = 0,
                    StrafeSpeed = (speedVector.Length > GetWizardMaxStrafeSpeed(_self) ? GetWizardMaxStrafeSpeed(_self) : speedVector.Length) * Math.Sin(angle)
                };
            }

            var coeff = strafeSpeed / forwardSpeed;

            var maxStrafe = GetWizardMaxStrafeSpeed(_self);
            var maxForward = forwardSpeed > 0 ? GetWizardMaxForwardSpeed(_self) : GetWizardMaxBackSpeed(_self);

            var realForwardSpeed = Math.Sqrt(1 / (1 / maxForward / maxForward + coeff * coeff / maxStrafe / maxStrafe));
            var realStrafeSpeed = realForwardSpeed * coeff;


            var fullSpeed = Math.Sqrt(realStrafeSpeed * realStrafeSpeed + realForwardSpeed * realForwardSpeed);
            if (fullSpeed > TOLERANCE)
            {
                var dist = _self.GetDistanceTo(x, y);
                var fullSpeedDist = fullSpeed * 1;
                if (fullSpeedDist > dist)
                {
                    var realSpeed = dist / 1;
                    var realSpeedCoeff = realSpeed / fullSpeed;
                    realForwardSpeed = fullSpeed * realSpeedCoeff / Math.Sqrt(1 + coeff * coeff);
                    realStrafeSpeed = fullSpeed * realSpeedCoeff * coeff / Math.Sqrt(1 + coeff * coeff);
                }
            }

            if (forwardSpeed * realForwardSpeed < 0) realForwardSpeed *= -1;
            if (strafeSpeed * realStrafeSpeed < 0) realStrafeSpeed *= -1;

            return new SpeedContainer()
            {
                Speed = realForwardSpeed,
                StrafeSpeed = realStrafeSpeed
            };
        }

        private void SendMessage()
        {
            if (!_self.IsMaster) return;

            _move.Messages = new Message[]
            {
                new Message(LaneType.Top, null, new byte[0]),
                new Message(LaneType.Middle, null, new byte[0]),
                new Message(LaneType.Middle, null, new byte[0]),
                new Message(LaneType.Bottom, null, new byte[0]),
            };
        }

        private LaneType? GetEmptyLane()
        {
            var laneTypes = new List<LaneType>()
            {
                LaneType.Top,
                LaneType.Middle,
                LaneType.Bottom
            };
            var emptyLines = new Dictionary<LaneType, bool>();
            foreach (var lt in laneTypes)
            {
                emptyLines.Add(lt, true);
            }


            foreach (var wizard in _world.Wizards.Where(x => x.Faction == _self.Faction))
            {
                foreach (var lt in laneTypes)
                {
                    if (IsStrongOnLine(wizard, lt)) emptyLines[lt] = false;
                }
            }

            foreach (var key in emptyLines.Keys)
            {
                if (emptyLines[key]) return key;
            }

            return null;
        }

        private void CheckMasterLine()
        {
            if (_self.IsMaster) return;
            if (_world.TickIndex == CHECK_MASTER_TIME)
            {
                var masterWizard = _world.Wizards.SingleOrDefault(x => x.Faction == _self.Faction && x.IsMaster);
                if (masterWizard != null)
                {
                    if (IsStrongOnLine(masterWizard, LaneType.Bottom))
                    {
                        _line = LaneType.Bottom;
                    }
                    else if (IsStrongOnLine(masterWizard, LaneType.Top))
                    {
                        _line = LaneType.Top;
                    }
                    else if (IsStrongOnLine(masterWizard, LaneType.Middle))
                    {
                        _line = LaneType.Middle;
                    }
                }
            }
        }

        private void CheckNeedChangeLine()
        {
            var newLine = GetNewLaneType();
            _needChangeLine = newLine != _line;

            if (IsOnBase() && _needChangeLine)
            {
                _needChangeLine = false;
                _line = newLine;
            }
        }

        private bool CanGoOnStaffRange(LivingUnit closestTarget)
        {
            if (closestTarget == null) return false;
            var newPoint = GetStraightRelaxPoint(closestTarget.X, closestTarget.Y,
                _game.StaffRange + closestTarget.Radius);
            var newMe = new Wizard(_self.Id, newPoint.X, newPoint.Y, _self.SpeedX, _self.SpeedY, _self.Angle,
                _self.Faction, _self.Radius, _self.Life, _self.MaxLife, _self.Statuses, _self.OwnerPlayerId, _self.IsMe,
                _self.Mana, _self.MaxMana, _self.VisionRange, _self.CastRange, _self.Xp, _self.Level, _self.Skills,
                _self.RemainingActionCooldownTicks, _self.RemainingCooldownTicksByAction, _self.IsMaster, _self.Messages);
            return !GetDangerousAnemies(newMe, _self.Radius * 2).Any(x => x is Wizard || x is Building);
        }

        private SkillType GetSkillTypeToLearn()
        {
            var skills = _self.Skills;
            if (skills.Length == _skillsOrder.Length)
            {
                return _skillsOrder[_skillsOrder.Length - 1];
            }
            return _skillsOrder[skills.Length];
        }

        private class BulletStartData
        {
            public BulletStartData(
                double startX,
                double startY,
                double castRange,
                double speedX,
                double speedY,
                double radius,
                double speed,
                Projectile bullet = null)
            {

                if (bullet != null)
                {
                    Angle = bullet.Angle;
                    StartX = bullet.X - bullet.SpeedX;
                    StartY = bullet.Y - bullet.SpeedY;
                }
                else
                {
                    StartX = startX;
                    StartY = startY;

                    Angle = speedX == 0 ? Math.PI / 2 : Math.Atan(speedY / speedX);
                    if (speedX < 0 && speedY < 0) Angle += Math.PI;
                    if (speedX < 0 && speedY > 0) Angle += Math.PI;
                }



                EndX = StartX + castRange * Math.Cos(Angle);
                EndY = StartY + castRange * Math.Sin(Angle);
                Radius = radius;
                CastRange = castRange;
                Speed = speed;
                Bullet = bullet;
            }

            public double Angle { get; set; }

            public Projectile Bullet { get; set; }

            public double Speed { get; set; }

            public double CastRange { get; set; }

            public double Radius { get; set; }

            public double StartX { get; set; }
            public double StartY { get; set; }

            public double EndX { get; private set; }
            public double EndY { get; private set; }
        }

        private class PathContainer
        {
            public IList<Point2D> Path { get; set; }
            public double Weight { get; set; }
        }


        private class Vector
        {
            public Point2D P1 { get; set; }
            public Point2D P2 { get; set; }

            public double X
            {
                get { return P2.X - P1.X; }
            }
            public double Y
            {
                get { return P2.Y - P1.Y; }
            }

            public double Length
            {
                get { return Math.Sqrt((P1.X - P2.X) * (P1.X - P2.X) + (P1.Y - P2.Y) * (P1.Y - P2.Y)); }
            }
        }

        private double GetScalarVectorMult(Vector v1, Vector v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }



        /**
    * ��������������� ����� ��� �������� ������� �� �����.
    */
        private class Point2D
        {
            public double X { get; set; }
            public double Y { get; set; }

            public Point2D(double x, double y)
            {
                X = x;
                Y = y;
            }

            public double getDistanceTo(double x, double y)
            {
                return Math.Sqrt((X - x) * (X - x) + (Y - y) * (Y - y));
            }

            public double getDistanceTo(Point2D point)
            {
                return getDistanceTo(point.X, point.Y);
            }

            public double getDistanceTo(Unit unit)
            {
                return getDistanceTo(unit.X, unit.Y);
            }
        }

        private class SpeedContainer
        {
            public double Speed { get; set; }
            public double StrafeSpeed { get; set; }
        }

        private class GoBonusResult
        {
            public bool IsGo { get; set; }
            public bool IsWoodCut { get; set; }
        }

    }
}