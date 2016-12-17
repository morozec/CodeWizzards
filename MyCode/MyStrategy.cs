using Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk.Model;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk;
using Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk.AStar;
using IPA.AStar;

namespace Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk
{
    public sealed class MyStrategy : IStrategy
    {


        //static MyStrategy()
        //{
        //    Debug.connect("localhost", 13579);
        //}

        private static double WAYPOINT_RADIUS = 100.0D;
        private static double LOW_HP_FACTOR = 0.33D;
        private static double HP_FACTOR_TO_GO_TO_TOWERS = 0.75D;
        private static double ROW_WIDTH = 400;
        private static double TOLERANCE = 1E-3;
        private static double BONUS_ADD_TIME = 300;
        private static double BONUS_ADD_TIME_ONE_ON_ONE = 200;
        private static double BONUS_ADD_TIME_PER_SUARE = 1.1;
        private static double LIGHT_SHOOTING_SQUARE_WEIGHT = 3;
        private static double STRONG_SHOOTING_SQUARE_WEIGHT = 7;
        private static double CLOSE_TO_WIN_DISTANCE = 1200;
        private static double CLOSE_TO_TOWER_DISTANCE = 700;
        private static double BERSERK_COEFF = 1.1;
        private static double TOWER_HP_FACTOR = 0.75;
        private static double COEFF_TO_RUN_FOR_WEAK = 1.2;
        
        private IDictionary<LaneType, Point2D[]> _waypointsByLine = new Dictionary<LaneType, Point2D[]>();
        private Point2D[] _cheatingWaypointsByLine;

        private Random _random;

        private LaneType _line;
        private bool _isLineSet = false;

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
        private bool _isAStarBuilt = false;
        private double _gotBonus0Time = 0d;
        private double _gotBonus1Time = 0d;

        private Point2D[] _bonusPoints = new Point2D[2] { new Point2D(1200d, 1200d), new Point2D(2800d, 2800d) };


        private IList<Building> _myBuildings;
        private IList<Building> _anemyBuildings;
        private IList<bool> _IsMyBuildingAlive;
        private IList<bool> _IsAnemyBuildingAlive;

        private IList<Point> _lastTickPath;
        private Point2D _lastTickResPoint;
        private Point2D _thisTickResPoint;
        private Point2D _behindBasePoint =new Point2D(3800, 200);

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

        private IDictionary<LaneType, IList<long>> _myWizards;
        private IDictionary<LaneType, IList<long>> _anemyWizards;

        private List<Wizard> _allMyWizards;
        private List<Wizard> _allAnemyWizards;
        private List<Wizard> _seenAnemyWizards;

        private bool _isBerserkTarget = false;
        private bool _isOneOneOne = false;

        //private bool _isCheatingStrategy = false;
         

        private readonly SkillType[] _agressiveSkillsOrder = new SkillType[]
        {

            SkillType.StaffDamageBonusPassive1,
            SkillType.StaffDamageBonusAura1,
            SkillType.StaffDamageBonusPassive2,
            SkillType.StaffDamageBonusAura2,
            SkillType.Fireball,

            SkillType.RangeBonusPassive1,
            SkillType.RangeBonusAura1,
            SkillType.RangeBonusPassive2,
            SkillType.RangeBonusAura2,
            SkillType.AdvancedMagicMissile,

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

        private readonly SkillType[] _devensiveSkillsOrder = new SkillType[]
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


        private readonly IList<SkillType[]> _commonSkillsOrder = new List<SkillType[]>()
        {
            new[]
            {
                SkillType.StaffDamageBonusPassive1,
                SkillType.StaffDamageBonusAura1,
                SkillType.StaffDamageBonusPassive2,
                SkillType.StaffDamageBonusAura2,
                SkillType.Fireball,

                SkillType.MovementBonusFactorPassive1,
                SkillType.MovementBonusFactorAura1,
                SkillType.MovementBonusFactorPassive2,
                SkillType.MovementBonusFactorAura2,
                SkillType.Haste,

                SkillType.MagicalDamageBonusPassive1,
                SkillType.MagicalDamageBonusAura1,
                SkillType.MagicalDamageBonusPassive2,
                SkillType.MagicalDamageBonusAura2,
                SkillType.FrostBolt,

                SkillType.RangeBonusPassive1,
                SkillType.RangeBonusAura1,
                SkillType.RangeBonusPassive2,
                SkillType.RangeBonusAura2,
                SkillType.AdvancedMagicMissile,

                SkillType.MagicalDamageAbsorptionPassive1,
                SkillType.MagicalDamageAbsorptionAura1,
                SkillType.MagicalDamageAbsorptionPassive2,
                SkillType.MagicalDamageAbsorptionAura2,
                SkillType.Shield,
            },

            new[]
            {
                SkillType.MovementBonusFactorPassive1,
                SkillType.MovementBonusFactorAura1,
                SkillType.MovementBonusFactorPassive2,
                SkillType.MovementBonusFactorAura2,
                SkillType.Haste,

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

                SkillType.RangeBonusPassive1,
                SkillType.RangeBonusAura1,
                SkillType.RangeBonusPassive2,
                SkillType.RangeBonusAura2,
                SkillType.AdvancedMagicMissile,

                SkillType.MagicalDamageAbsorptionPassive1,
                SkillType.MagicalDamageAbsorptionAura1,
                SkillType.MagicalDamageAbsorptionPassive2,
                SkillType.MagicalDamageAbsorptionAura2,
                SkillType.Shield,
            },

            new[]
            {
                SkillType.MagicalDamageBonusPassive1,
                SkillType.MagicalDamageBonusAura1,
                SkillType.MagicalDamageBonusPassive2,
                SkillType.MagicalDamageBonusAura2,
                SkillType.FrostBolt,

                SkillType.MovementBonusFactorPassive1,
                SkillType.MovementBonusFactorAura1,
                SkillType.MovementBonusFactorPassive2,
                SkillType.MovementBonusFactorAura2,
                SkillType.Haste,

                SkillType.StaffDamageBonusPassive1,
                SkillType.StaffDamageBonusAura1,
                SkillType.StaffDamageBonusPassive2,
                SkillType.StaffDamageBonusAura2,
                SkillType.Fireball,
            
                SkillType.RangeBonusPassive1,
                SkillType.RangeBonusAura1,
                SkillType.RangeBonusPassive2,
                SkillType.RangeBonusAura2,
                SkillType.AdvancedMagicMissile,

                SkillType.MagicalDamageAbsorptionPassive1,
                SkillType.MagicalDamageAbsorptionAura1,
                SkillType.MagicalDamageAbsorptionPassive2,
                SkillType.MagicalDamageAbsorptionAura2,
                SkillType.Shield,
            },


             new[]
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

                SkillType.MovementBonusFactorPassive1,
                SkillType.MovementBonusFactorAura1,
                SkillType.MovementBonusFactorPassive2,
                SkillType.MovementBonusFactorAura2,
                SkillType.Haste,

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
            },

             new[]
            {
                SkillType.MagicalDamageAbsorptionPassive1,
                SkillType.MagicalDamageAbsorptionAura1,
                SkillType.MagicalDamageAbsorptionPassive2,
                SkillType.MagicalDamageAbsorptionAura2,
                SkillType.Shield,

                SkillType.StaffDamageBonusPassive1,
                SkillType.StaffDamageBonusAura1,
                SkillType.StaffDamageBonusPassive2,
                SkillType.StaffDamageBonusAura2,
                SkillType.Fireball,

                SkillType.MovementBonusFactorPassive1,
                SkillType.MovementBonusFactorAura1,
                SkillType.MovementBonusFactorPassive2,
                SkillType.MovementBonusFactorAura2,
                SkillType.Haste,

                SkillType.MagicalDamageBonusPassive1,
                SkillType.MagicalDamageBonusAura1,
                SkillType.MagicalDamageBonusPassive2,
                SkillType.MagicalDamageBonusAura2,
                SkillType.FrostBolt,

                SkillType.RangeBonusPassive1,
                SkillType.RangeBonusAura1,
                SkillType.RangeBonusPassive2,
                SkillType.RangeBonusAura2,
                SkillType.AdvancedMagicMissile,
            },
        };


        public void Move(Wizard self, World world, Game game, Move move)
         {
             //Debug.beginPost();
            //for (int i = 0; i < _n; ++i)
            //{
            //    for (int j = 0; j < _m; ++j)
            //    {е
            //        var dist = _self.GetDistanceTo(_table[i, j].X, _table[i, j].Y);
            //        if (dist > _self.CastRange) continue;

            //        if (_table[i, j].Weight >= LIGHT_SHOOTING_SQUARE_WEIGHT && _table[i, j].Weight < 999999)
            //        {
            //            Debug.rect(_table[i, j].X, _table[i, j].Y, _table[i, j].X + _table[i, j].Side,
            //                _table[i, j].Y + _table[i, j].Side, 150);
            //        }
            //    }
            //}



            InitializeTick(self, world, game, move);
            InitializeStrategy(self, game);

            _move.SkillToLearn = GetSkillTypeToLearn();
            UpdateBulletStartDatas();
            SendMessage();

            InitShiled();
            InitHaste();
            
            InitializeLineActions();
            

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


            if ((_world.TickIndex - _gotBonus0Time) % _game.BonusAppearanceIntervalTicks > 0)
            {
                _gotBonus0Time += (_world.TickIndex - _gotBonus0Time) % _game.BonusAppearanceIntervalTicks;
            }
            if ((_world.TickIndex - _gotBonus1Time) % _game.BonusAppearanceIntervalTicks > 0)
            {
                _gotBonus1Time += (_world.TickIndex - _gotBonus1Time) % _game.BonusAppearanceIntervalTicks;
            }

        

            if (!_isAStarBuilt)
            {
                MakeDinamycAStar();
                _isAStarBuilt = true;
            }
            UpdateDinamycAStar();
            UpdateLastTickPathTrees();
            

            _selfBase =
                      _world.Buildings.Single(x => x.Faction == _self.Faction && x.Type == BuildingType.FactionBase);

            _anemyBaseX = _world.Width - _selfBase.X;
            _anemyBaseY = _world.Height - _selfBase.Y;


            var nearestStaffTarget = GetNearestStaffRangeTarget(_self);
            var shootingTarget = _isOneOneOne ? GetOneOnOneShootingTarget() :
                GetAgressiveLineShootingTarget();

            var goBonusResult = CheckAndGoForBonus(nearestStaffTarget, shootingTarget);

            _gotBonus0Time++;
            _gotBonus1Time++;

            LivingUnit turnTarget = null;
            GoToResult goToResult;
            
            if (nearestStaffTarget != null)
            {
                if (!goBonusResult.IsGo)
                {
                    //move.Turn = angle;
                    var cooldown = Math.Max(_self.RemainingActionCooldownTicks,
                        _self.RemainingCooldownTicksByAction[(int) ActionType.Staff]);

                    if (nearestStaffTarget is Building &&
                        (nearestStaffTarget as Building).Type == BuildingType.FactionBase &&
                        cooldown > 10 && (_behindBasePoint.getDistanceTo(_self) > 100))
                    {
                        goToResult =  GoTo(_behindBasePoint, _self.Radius * 2, _self.Radius * 2);
                    }
                    else
                    {
                        goToResult = new GoToResult()
                        {
                            WoodCuTree = null,
                            X = _self.X,
                            Y = _self.Y
                        };
                    }

                }
                else
                {
                    goToResult = goBonusResult.GoToResult;
                }

                turnTarget = nearestStaffTarget;


                double angle = self.GetAngleTo(nearestStaffTarget);
                if (Math.Abs(angle) <= _game.StaffSector / 2.0D)
                {
                    InitializeShootingAction(nearestStaffTarget, true);
                }

                //var bullet = GetBulletFlyingInMe();
                //if (!goBonusResult.IsGo && (!canGoOnStaffRange || bullet != null || NeedGoBack())) // && NeedGoBack(CanGoOnStaffRange(nearestStaffTarget))
                //{
                //    MakeGoBack(bullet);
                //}

            }
            else
            {
                
                var closestTarget = GetClosestTarget();
               
                if (shootingTarget != null)
                {
                    turnTarget = shootingTarget;
                    if (!goBonusResult.IsGo)
                    {
                        //TODO!!! Теоретически closestTarget м.б. null
                        var isCalmMinion = (shootingTarget is Minion && (shootingTarget as Minion).Faction == Faction.Neutral &&
                                            IsCalmNeutralMinion(shootingTarget as Minion));

                        //var anmeyMinions =
                        //    _world.Minions.Where(
                        //        x =>
                        //            x.Faction != _self.Faction &&
                        //            (x.Faction != Faction.Neutral || !IsCalmNeutralMinion(x)));

                        var isFirstTower = _isOneOneOne && shootingTarget is Building &&
                                           (shootingTarget as Building).Type == BuildingType.GuardianTower &&
                                           GetAliveAnemyTowers(LaneType.Bottom).Count == 2 && _world.TickIndex < 1450;

                        var isSecondTower = _isOneOneOne && shootingTarget is Building &&
                                           (shootingTarget as Building).Type == BuildingType.GuardianTower &&
                                           GetAliveAnemyTowers(LaneType.Bottom).Count == 1;

                        var nearTower =
                            _world.Buildings.FirstOrDefault(
                                x =>
                                    x.Type == BuildingType.GuardianTower && x.Faction != _self.Faction &&
                                    IsOkDistanceToShoot(_self, x, 0d) && IsStrongOnLine(x, _line));

                        var isFarTarget = _self.GetDistanceTo(_anemyBaseX, _anemyBaseY) <
                                          shootingTarget.GetDistanceTo(_anemyBaseX, _anemyBaseY);

                        var nearToShotingTargetWizards =
                            _world.Wizards.Where(
                                x =>
                                    x.Faction == _self.Faction && !x.IsMe &&
                                    x.GetDistanceTo(shootingTarget) <= _game.StaffRange + shootingTarget.Radius);
                                      
                        if (isCalmMinion || isFirstTower || isSecondTower && nearToShotingTargetWizards.Count() >=3)
                        {
                            _thisTickResPoint = new Point2D(_self.X, _self.Y);
                            goToResult = new GoToResult()
                            {
                                WoodCuTree = null,
                                X = _self.X,
                                Y = _self.Y
                            };
                              
                        }
                       
                        else if (_isOneOneOne && nearTower != null)
                        {
                            _thisTickResPoint = new Point2D(nearTower.X, nearTower.Y);
                            goToResult = GoTo(
                                new Point2D(nearTower.X, nearTower.Y),
                                _game.StaffRange + nearTower.Radius - TOLERANCE,
                                _game.StaffRange + nearTower.Radius - TOLERANCE);
                        }
                        else if (isFarTarget)
                        {
                            var nextWaypoint = GetNextWaypoint();
                            _thisTickResPoint = nextWaypoint;
                            goToResult = GoTo(nextWaypoint, _self.Radius * 2, 0d);
                        }
                        else
                        {
                            _thisTickResPoint = new Point2D(shootingTarget.X, shootingTarget.Y);
                            goToResult = GoTo(
                                new Point2D(shootingTarget.X, shootingTarget.Y),
                                _game.StaffRange + shootingTarget.Radius - TOLERANCE,
                                _game.StaffRange + shootingTarget.Radius - TOLERANCE);
                        }
                    }
                    else
                    {
                        goToResult = goBonusResult.GoToResult;
                    }
                                        
                    InitializeShootingAction(shootingTarget, false);
                    
                }
                else
                {
                    if (!goBonusResult.IsGo)
                    {
                       
                        var nearestBaseTarget = GetNearestMyBaseAnemy(_line);
                        if (nearestBaseTarget != null && !_isOneOneOne)
                        {
                            if (closestTarget != null && _self.GetDistanceTo(closestTarget) < _self.CastRange*1.5)
                                turnTarget = nearestBaseTarget;

                            var radius = _self.CastRange - nearestBaseTarget.Radius +
                                            _game.MagicMissileRadius * 1.5;
                            var radiusPoint = GetRadiusPoint(ROW_WIDTH * 0.75, radius, nearestBaseTarget);

                            _thisTickResPoint = new Point2D(nearestBaseTarget.X, nearestBaseTarget.Y);
                            if (radiusPoint != null)
                            {
                                goToResult = GoTo(radiusPoint, _self.Radius * 2, _self.Radius * 2);
                                _lastTickResPoint = new Point2D(nearestBaseTarget.X, nearestBaseTarget.Y);
                            }
                            else
                            {
                                goToResult = GoTo(
                                    new Point2D(nearestBaseTarget.X, nearestBaseTarget.Y),
                                    _game.StaffRange + nearestBaseTarget.Radius + 10000 * TOLERANCE,
                                    _game.StaffRange + nearestBaseTarget.Radius + 10000 * TOLERANCE);
                            }

                        }
                        else
                        {
                            var nextWaypoint = GetNextWaypoint();

                            if (_isOneOneOne && nextWaypoint.X > _cheatingWaypointsByLine[2].X)
                            {
                                for (int i = 0; i < _anemyBuildings.Count; ++i)
                                {
                                    if (_anemyBuildings[i].X > _world.Width - 400 && _anemyBuildings[i].Y > 2000)
                                    {
                                        if (_IsAnemyBuildingAlive[i])
                                        {
                                            nextWaypoint = _cheatingWaypointsByLine[3];
                                            break;
                                        }
                                    }
                                }
                            }

                            else if (_isOneOneOne && nextWaypoint.X > _cheatingWaypointsByLine[3].X)
                            {
                                for (int i = 0; i < _anemyBuildings.Count; ++i)
                                {
                                    if (_anemyBuildings[i].X > _world.Width - 400 && _anemyBuildings[i].Y < 2000)
                                    {
                                        if (_IsAnemyBuildingAlive[i])
                                        {
                                            nextWaypoint = _cheatingWaypointsByLine[4];
                                            break;
                                        }
                                    }
                                }
                            }

                            _thisTickResPoint = nextWaypoint;
                            goToResult = GoTo(nextWaypoint, _self.Radius * 2, 0d);
                        }
                        
                    }
                    else
                    {
                        if (closestTarget != null && _self.GetDistanceTo(closestTarget) < _self.CastRange*1.5)
                            turnTarget = closestTarget;

                        goToResult = goBonusResult.GoToResult;
                    }
                }
            }

            var bullet = GetBulletFlyingInMe();

            

            var speedContainer = GetSpeedContainer(goToResult.X, goToResult.Y);
            var speedX = speedContainer.Speed * Math.Cos(_self.Angle) +
                         speedContainer.StrafeSpeed * Math.Cos(_self.Angle + Math.PI / 2);
            var speedY = speedContainer.Speed * Math.Sin(_self.Angle) +
                        speedContainer.StrafeSpeed * Math.Sin(_self.Angle + Math.PI / 2);
            var selfNextTickX = _self.X + speedX;
            var selfNextTickY = _self.Y + speedY;


            //Debug.circle(nextX, nextY, 10, 150);
            
            if (bullet != null)
            {
                var time = GetBulletTime(bullet, _self);

                var canGoBack = CanGoBack(_self, bullet, time, false);
                var canGoLeft = CanGoLeft(_self, bullet, time, false);
                var canGoRight = CanGoRight(_self, bullet, time, false);
                var canGoForward = CanGoForward(_self, bullet, time, false);
                if (canGoBack)
                {
                    _move.Speed = -GetWizardMaxBackSpeed(_self);
                    _move.StrafeSpeed = 0;
                    return;
                }
                else if (canGoLeft)
                {
                    _move.Speed = 0;
                    _move.StrafeSpeed = -GetWizardMaxStrafeSpeed(_self);
                    _move.Turn = 0;
                    return;
                }
                else if (canGoRight)
                {
                    _move.Speed = 0;
                    _move.StrafeSpeed = GetWizardMaxStrafeSpeed(_self);
                    _move.Turn = 0;
                    return;
                }
                else if (canGoForward)
                {
                    _move.Speed = GetWizardMaxForwardSpeed(_self);
                    _move.StrafeSpeed = 0;
                    return;
                }
                //else
                //{
                //    goToResult = GoBack();
                //    speedContainer = GetSpeedContainer(goToResult.X, goToResult.Y);
                //}
            }

            IList<Wizard> goBackWizards;
            double addTurnAngle = 0;
            var canGoOnStaffRange = CanGoToStaffRange(shootingTarget, selfNextTickX, selfNextTickY, out goBackWizards);
            var goBack = false;
            if (!goBonusResult.IsGo && (!canGoOnStaffRange || NeedGoBack()))
            {
                goToResult = GoBack();
                speedContainer = GetSpeedContainer(goToResult.X, goToResult.Y);
                goBack = true;

                
                if (!_isOneOneOne && goBackWizards.Any())
                {
                    var turnTime = 0d;
                    if (turnTarget != null)
                    {
                        turnTime = GetTurnTime(_self, turnTarget);
                    }
                    if (turnTime <= GetShootingCooldown(_self))
                    {
                        var orderedGoBackWizards = goBackWizards.OrderBy(GetShootingCooldown);
                        turnTarget = orderedGoBackWizards.First();

                        if (_self.GetAngleTo(turnTarget) < 0) addTurnAngle = Math.PI / 2;
                        else addTurnAngle = -Math.PI / 2;
                        
                    }
                }
            }
            
        
            var woodCutTree = goToResult.WoodCuTree;

            if (woodCutTree != null)
            {
                var angle = _self.GetAngleTo(woodCutTree);
                _move.Turn = angle;
                var distance = _self.GetDistanceTo(woodCutTree) - woodCutTree.Radius;

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

            else if (turnTarget != null)
            {
                _move.Turn = _self.GetAngleTo(turnTarget.X, turnTarget.Y) + addTurnAngle;
            }
            else if (!goBack)
            {
                double turnPointX = goToResult.X;
                double turnPointY = goToResult.Y;

                #region Дерево

                //var nextWayPoint = GetNextWaypoint();
                var neutralMinions = _world.Minions.Where(x => x.Faction == Faction.Neutral);
                var nextWayPoint = GetNextWaypoint();


                if (_isOneOneOne && nextWayPoint.X== _cheatingWaypointsByLine[3].X && nextWayPoint.Y == _cheatingWaypointsByLine[3].Y)
                {
                    var closeTrees = _trees.Where(x => IsOkDistanceToShoot(_self, x, 0));
                    var intersectingTrees = new List<Tree>();
                    foreach (var tree in closeTrees)
                    {
                        if (Square.Intersect(_self.X, _self.Y, nextWayPoint.X, nextWayPoint.Y, tree.X, tree.Y,
                            _self.Radius, tree.Radius))
                        {
                            intersectingTrees.Add(tree);
                        }
                    }

                    var minHpTrees = intersectingTrees.OrderBy(x => x.GetDistanceTo(_self));
                    var minHpTree = minHpTrees.FirstOrDefault();
                    if (minHpTree != null)
                    {
                        turnPointX = minHpTree.X;
                        turnPointY = minHpTree.Y;
                        if (Math.Abs(_self.GetAngleTo(minHpTree)) <= _game.StaffSector/2d)
                        {
                            var distance = _self.GetDistanceTo(minHpTree) - minHpTree.Radius;
                            if (_self.RemainingCooldownTicksByAction[(int) ActionType.Staff] == 0 &&
                                distance <= _game.StaffRange)
                            {
                                _move.Action = ActionType.Staff;
                            }
                            else if (_self.RemainingCooldownTicksByAction[(int)ActionType.MagicMissile] == 0)
                            {
                                _move.Action = ActionType.MagicMissile;
                                _move.MinCastDistance = _self.GetDistanceTo(minHpTree) - minHpTree.Radius +
                                                   _game.MagicMissileRadius;
                            }
                            _move.CastAngle = _self.GetAngleTo(minHpTree);
                           
                        }
                    }

                }

               
                #endregion
               

                else if (goBonusResult.IsGo)
                {
                    turnPointX = _bonusPoints[0].getDistanceTo(_self) < _bonusPoints[1].getDistanceTo(_self)
                        ? _bonusPoints[0].X
                        : _bonusPoints[1].X;
                    turnPointY = _bonusPoints[0].getDistanceTo(_self) < _bonusPoints[1].getDistanceTo(_self)
                        ? _bonusPoints[0].Y
                        : _bonusPoints[1].Y;
                }

                var isInPoint = Math.Abs(_self.X - turnPointX) < TOLERANCE &&
                                Math.Abs(_self.Y - turnPointY) < TOLERANCE;
                if (!isInPoint) _move.Turn = _self.GetAngleTo(turnPointX, turnPointY);
            }


            var isOtherLineTower = _anemyBuildings.Any(
                x =>
                    x.Type == BuildingType.GuardianTower && !IsStrongOnLine(x, _line) &&
                    x.GetDistanceTo(goToResult.X, goToResult.Y) <= x.AttackRange);

            var anemyBase = _anemyBuildings.Single(
                x =>
                    x.Type == BuildingType.FactionBase);
            var isBase =  GetAliveAnemyTowers(_line).Count > 0 &&
                   anemyBase.GetDistanceTo(goToResult.X, goToResult.Y) <= anemyBase.AttackRange;

            if (_isOneOneOne && (isOtherLineTower || isBase))
            {
                speedContainer = new SpeedContainer() { Speed = 0, StrafeSpeed = 0 };
            }

            _move.Speed = speedContainer.Speed;
            _move.StrafeSpeed = speedContainer.StrafeSpeed;



        }

        private void InitializeLineActions()
        {
            UpdateWizardsLanes();
            if (_isOneOneOne)
            {
                //if (_seenAnemyWizards.Count == 5)
                //{
                //    if (_self.Id % 5 == 2 &&
                //        (_anemyWizards[LaneType.Middle].Count == 5 || _anemyWizards[LaneType.Middle].Count <= 2))
                //    {
                //        _line = LaneType.Middle;
                //    }

                //    if (_anemyWizards[LaneType.Bottom].Count >= 2 && _anemyWizards[_line].Count > 0 &&
                //        (_self.Id % 5 == 1 || _self.Id % 5 == 2))
                //    {
                //        _line = LaneType.Middle;
                //    }
                //}
                return;
            }

            var isNearToBase = _self.X <= 2 * ROW_WIDTH && _self.Y >= _world.Height - 2 * ROW_WIDTH;

            if (_world.TickIndex <= 600)
            {
                var line = GetAgressiveLineToGo(_world.TickIndex < 600);
                if (line != null)
                {
                    _line = line.Value;
                    _isLineSet = true;
                }
            }
            else if (isNearToBase)
            {
                _line = GetNearToBaseLine();
            }


            if (_world.TickIndex > 2000)
            {
                if (_bonusPoints[0].getDistanceTo(_self) < (_self.Radius + _game.BonusRadius) * 2)
                {
                    _line = GetOptimalLine(LaneType.Bottom);
                    _isLineSet = false;
                }
                if (_bonusPoints[1].getDistanceTo(_self) < (_self.Radius + _game.BonusRadius) * 2)
                {
                    _line = GetOptimalLine(LaneType.Top);
                    _isLineSet = false;
                }
            }


            if (!_isLineSet && _world.TickIndex > 600)
            {
                if (IsStrongOnLine(_self, _line)) _isLineSet = true;
            }
        }

        private void UpdateWizardsLanes()
        {
            #region Удаление моих мертвых
            var myDeadWizards = new List<Wizard>();
            foreach (var wizard in _allMyWizards)
            {
                if (!_world.Wizards.Any(x => x.Id == wizard.Id))
                {
                    myDeadWizards.Add(wizard);
                }
            }

            foreach (var myDeadWizard in myDeadWizards)
            {
                foreach (var item in _myWizards)
                {
                    _myWizards[item.Key].Remove(myDeadWizard.Id);
                }
            }
            #endregion

            foreach (var wizard in _world.Wizards.Where(x => x.Faction != _self.Faction))
            {
                _allAnemyWizards.RemoveAll(x => x.Id == wizard.Id);
                _allAnemyWizards.Add(wizard);

                if (!_seenAnemyWizards.Any(x => x.Id == wizard.Id))
                {
                    _seenAnemyWizards.Add(wizard);
                }
            }

            var anemyDeadWizards = new List<Wizard>();
            for (int i = _allAnemyWizards.Count - 1; i >= 0; --i)
            {
                var wizard = _allAnemyWizards[i];
                if (!_world.Wizards.Any(x => x.Id == wizard.Id))
                {
                    if (IsPointVisible(wizard.X, wizard.Y, 12d)) anemyDeadWizards.Add(wizard);
                    _allAnemyWizards.Remove((wizard));
                }
            }

            foreach (var anemyDeadWizard in anemyDeadWizards)
            {
                foreach (var item in _anemyWizards)
                {
                    _anemyWizards[item.Key].Remove(anemyDeadWizard.Id);
                }
            }



            foreach (var wizard in _world.Wizards.Where(x => !x.IsMe))
            {
                var line = GetLineType(wizard);
                if (line == null) continue;
                if (wizard.Faction == _self.Faction)
                {
                    if (!_myWizards[line.Value].Any(x => x == wizard.Id))
                    {
                        _myWizards[line.Value].Add(wizard.Id);
                        foreach (var item in _myWizards.Where(x => x.Key != line.Value))
                        {
                            _myWizards[item.Key].Remove(wizard.Id);
                        }
                    }
                }
                else
                {
                    if (!_anemyWizards[line.Value].Any(x => x == wizard.Id))
                    {
                        _anemyWizards[line.Value].Add(wizard.Id);
                        foreach (var item in _anemyWizards.Where(x => x.Key != line.Value))
                        {
                            _anemyWizards[item.Key].Remove(wizard.Id);
                        }
                    }
                }
            }
        }

        private bool IsWeakWizard(LivingUnit unit)
        {
            var shootingCoeff = GetMyLineType(_line) == LineType.Defensive ? 1d : 1.75d;
            var wizard = unit as Wizard;
            return wizard != null && wizard.Life <= GetShootingPower(_self) * shootingCoeff;
        }

        private double GetShootingPower(Wizard wizard)
        {
            var defaultDamage = _game.MagicMissileDirectDamage;
            double resultDamage = defaultDamage;

            if (wizard.Skills.Any(x => x == SkillType.MagicalDamageBonusAura2))
            {
                resultDamage += 4;
            }
            else if (wizard.Skills.Any(x => x == SkillType.MagicalDamageBonusPassive2))
            {
                resultDamage += 3;
            }
            else if (wizard.Skills.Any(x => x == SkillType.MagicalDamageBonusAura1))
            {
                resultDamage += 2;
            }
            else if (wizard.Skills.Any(x => x == SkillType.MagicalDamageBonusPassive1))
            {
                resultDamage += 1;
            }


            var nearWizards =
                _world.Wizards.Where(
                    w =>
                        w.Id != wizard.Id && w.Faction == wizard.Faction &&
                        w.GetDistanceTo(wizard) <= _game.AuraSkillRange);

            SkillType? maxAura = null;
            foreach (var w in nearWizards)
            {
                if (w.Skills.Contains(SkillType.MagicalDamageBonusAura2))
                {
                    maxAura = SkillType.MagicalDamageBonusAura2;
                    break;
                }
                if (w.Skills.Contains(SkillType.MagicalDamageBonusAura1))
                {
                    maxAura = SkillType.MagicalDamageBonusAura1;
                }
            }

            if (maxAura == SkillType.MagicalDamageBonusAura2)
            {
                resultDamage += 2;
            }
            else if (maxAura == SkillType.MagicalDamageBonusAura1)
            {
                resultDamage += 1;
            }

            if (wizard.Statuses.Any(x => x.Type == StatusType.Empowered))
            {
                resultDamage *= 1.5;
            }


            return resultDamage;
        }

        private double GetStaffPower(Wizard wizard)
        {
            var defaultDamage = _game.StaffDamage;
            double resultDamage = defaultDamage;

            if (wizard.Skills.Any(x => x == SkillType.StaffDamageBonusAura2))
            {
                resultDamage += 3 * 4;
            }
            else if (wizard.Skills.Any(x => x == SkillType.StaffDamageBonusPassive2))
            {
                resultDamage += 3 * 3;
            }
            else if (wizard.Skills.Any(x => x == SkillType.StaffDamageBonusAura1))
            {
                resultDamage += 3 * 2;
            }
            else if (wizard.Skills.Any(x => x == SkillType.StaffDamageBonusPassive1))
            {
                resultDamage += 3 * 1;
            }


            var nearWizards =
                _world.Wizards.Where(
                    w =>
                        w.Id != wizard.Id && w.Faction == wizard.Faction &&
                        w.GetDistanceTo(wizard) <= _game.AuraSkillRange);

            SkillType? maxAura = null;
            foreach (var w in nearWizards)
            {
                if (w.Skills.Contains(SkillType.StaffDamageBonusAura2))
                {
                    maxAura = SkillType.StaffDamageBonusAura2;
                    break;
                }
                if (w.Skills.Contains(SkillType.StaffDamageBonusAura1))
                {
                    maxAura = SkillType.StaffDamageBonusAura1;
                }
            }

            if (maxAura == SkillType.StaffDamageBonusAura2)
            {
                resultDamage += 3 * 2;
            }
            else if (maxAura == SkillType.StaffDamageBonusAura1)
            {
                resultDamage += 3 * 1;
            }

            if (wizard.Statuses.Any(x => x.Type == StatusType.Empowered))
            {
                resultDamage *= 1.5;
            }


            return resultDamage;
        }

        private void InitShiled()
        {
            if (_self.Skills.Any(x => x == SkillType.Shield) && !_self.Statuses.Any(x => x.Type == StatusType.Shielded))
            {
                var orderedWizards =
                    _world.Wizards.Where(
                        x => x.Faction == _self.Faction && !x.IsMe && !x.Statuses.Any(y => y.Type == StatusType.Shielded))
                        .OrderBy(x => x.GetDistanceTo(_self));
                var targetWizard = orderedWizards.FirstOrDefault();
                if (targetWizard == null) return;

                _move.Action = ActionType.Shield;
                _move.StatusTargetId = targetWizard.Id;
            }
        }

        private void InitHaste()
        {
            if (_self.Skills.Any(x => x == SkillType.Shield) && !_self.Statuses.Any(x => x.Type == StatusType.Hastened))
            {
                var orderedWizards =
                    _world.Wizards.Where(
                        x => x.Faction == _self.Faction && !x.IsMe && !x.Statuses.Any(y => y.Type == StatusType.Hastened))
                        .OrderBy(x => x.GetDistanceTo(_self));
                var targetWizard = orderedWizards.FirstOrDefault();
                if (targetWizard == null) return;

                _move.Action = ActionType.Haste;
                _move.StatusTargetId = targetWizard.Id;
            }
        }
      

        private void InitializeShootingAction(LivingUnit shootingTarget, bool canStaff)
        {
            double angle = _self.GetAngleTo(shootingTarget);
            if (Math.Abs(angle) >= _game.StaffSector / 2.0D) return;


            double distance = _self.GetDistanceTo(shootingTarget);
            var shootingWizard = shootingTarget as Wizard;
            var shootingBuilding = shootingTarget as Building;

            var canShootWithMissle = shootingWizard != null
                ? CanShootWizardWithMissleNoCooldown(_self, _self.X, _self.Y, shootingWizard, 0, true, true, true)
                : IsOkDistanceToShoot(_self, shootingTarget,0d);

            var canShootWithFrostbolt = shootingWizard != null
                                        && CanShootWizardWithFrostboltNoCooldown(_self, _self.X, _self.Y, shootingWizard, 0, true, true, true);


            var canFireballWizard = shootingWizard != null &&
                                    CanShootWizardWithFireballNoCooldown(_self, _self.X, _self.Y, shootingWizard, 0, true, true, true);
            var canFireballBuilding = shootingBuilding != null;
            var wantedDist = distance + shootingTarget.Radius;
            var realDist = shootingTarget is Building ? (wantedDist > _self.CastRange ? _self.CastRange : wantedDist) : distance;
            var isSafeToShootFireball =
                !_world.Wizards.Any(
                    x => x.Faction == _self.Faction && realDist - x.Radius <= _game.FireballExplosionMinDamageRange);

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
                     (canFireballWizard || canFireballBuilding || canFiballUnit) && isSafeToShootFireball &&
                     _self.RemainingCooldownTicksByAction[(int)ActionType.Fireball] == 0 && shootingTarget.Faction != Faction.Neutral
                    && !IsBlockingTree(_self, shootingTarget, _game.FireballRadius))
            {
                _move.Action = ActionType.Fireball;
                _move.CastAngle = angle;
                _move.MinCastDistance = shootingTarget is Building ? realDist - 1 : distance - shootingTarget.Radius + _game.FireballRadius;
            }
            else if (_self.RemainingActionCooldownTicks == 0 && _self.Skills.Any(x => x == SkillType.FrostBolt) &&
                     canShootWithFrostbolt &&
                     _self.RemainingCooldownTicksByAction[(int)ActionType.FrostBolt] == 0
                && !IsBlockingTree(_self, shootingTarget, _game.FrostBoltRadius))
            {
                _move.Action = ActionType.FrostBolt;
                _move.CastAngle = angle;
                _move.MinCastDistance = distance - shootingTarget.Radius + _game.FrostBoltRadius;
            }
            else if (_self.RemainingActionCooldownTicks == 0 && canShootWithMissle &&
                     _self.RemainingCooldownTicksByAction[(int)ActionType.MagicMissile] == 0)
            {
                _move.Action = ActionType.MagicMissile;
                _move.CastAngle = angle;
                _move.MinCastDistance = distance - shootingTarget.Radius + _game.MagicMissileRadius;
            }
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
      
        
        /// <summary>
        /// Может ли волшебник отойти от пули bsd. time - время на отход
        /// </summary>
        /// <param name="target"></param>
        /// <param name="bsd"></param>
        /// <param name="time"></param>
        /// <param name="ignoreGoStraightPoint"></param>
        /// <returns></returns>
        private bool CanGoBack(Wizard target, BulletStartData bsd, int time, bool ignoreGoStraightPoint)
        {
            double newX = target.X;
            double newY = target.Y;
            if (ignoreGoStraightPoint)
            {
                newX = target.X + GetWizardMaxStrafeSpeed(target) * time * Math.Cos(target.Angle - Math.PI);
                newY = target.Y + GetWizardMaxStrafeSpeed(target) * time * Math.Sin(target.Angle - Math.PI);
            }
            else
            {
                for (int i = 0; i <= time; ++i)
                {
                    newX = target.X + GetWizardMaxStrafeSpeed(target) * i * Math.Cos(target.Angle - Math.PI);
                    newY = target.Y + GetWizardMaxStrafeSpeed(target) * i * Math.Sin(target.Angle - Math.PI);

                    if (GetGoStraightPoint(newX, newY, 0) == null) break;
                }
            }

            if (newX < target.Radius || newY < target.Radius || newX > _world.Width - target.Radius ||
                  newY > _world.Height - target.Radius)
                return false;

            var isIntersect = Square.Intersect(
                  bsd.StartX,
                  bsd.StartY,
                  bsd.EndX,
                  bsd.EndY,
                  newX,
                  newY,
                  bsd.Radius,
                  target.Radius);

            return !isIntersect;
        }

        private bool CanGoForward(Wizard target, BulletStartData bsd, int time, bool ignoreGoStraightPoint)
        {

            double newX = target.X;
            double newY = target.Y;
            if (ignoreGoStraightPoint)
            {
                newX = target.X + GetWizardMaxForwardSpeed(target)*time*Math.Cos(target.Angle);
                newY = target.Y + GetWizardMaxForwardSpeed(target)*time*Math.Sin(target.Angle);
            }
            else
            {
                for (int i = 0; i <= time; ++i)
                {
                    newX = target.X + GetWizardMaxForwardSpeed(target) * i * Math.Cos(target.Angle);
                    newY = target.Y + GetWizardMaxForwardSpeed(target) * i * Math.Sin(target.Angle);
                    
                    if (GetGoStraightPoint(newX, newY, 0) == null) break;
                }
            }

            if (newX < target.Radius || newY < target.Radius || newX > _world.Width - target.Radius ||
                  newY > _world.Height - target.Radius)
                return false;

            var isIntersect = Square.Intersect(
                  bsd.StartX,
                  bsd.StartY,
                  bsd.EndX,
                  bsd.EndY,
                  newX,
                  newY,
                  bsd.Radius,
                  target.Radius);

            return !isIntersect;
        }
        
        private bool CanGoLeft(Wizard target, BulletStartData bsd, int time, bool ignoreGoStraightPoint)
        {
            double newX = target.X;
            double newY = target.Y;
            if (ignoreGoStraightPoint)
            {
                newX = target.X + GetWizardMaxStrafeSpeed(target) * time * Math.Cos(target.Angle - Math.PI / 2);
                newY = target.Y + GetWizardMaxStrafeSpeed(target) * time * Math.Sin(target.Angle - Math.PI / 2);
            }
            else
            {
                for (int i = 0; i <= time; ++i)
                {
                    newX = target.X + GetWizardMaxStrafeSpeed(target) * i * Math.Cos(target.Angle - Math.PI / 2);
                    newY = target.Y + GetWizardMaxStrafeSpeed(target) * i * Math.Sin(target.Angle - Math.PI / 2);

                    if (GetGoStraightPoint(newX, newY, 0) == null) break;
                }
            }

            if (newX < target.Radius || newY < target.Radius || newX > _world.Width - target.Radius ||
                  newY > _world.Height - target.Radius)
                return false;

            var isIntersect = Square.Intersect(
                  bsd.StartX,
                  bsd.StartY,
                  bsd.EndX,
                  bsd.EndY,
                  newX,
                  newY,
                  bsd.Radius,
                  target.Radius);

            return !isIntersect;
        }

        private bool CanGoRight(Wizard target, BulletStartData bsd, int time, bool ignoreGoStraightPoint)
        {

            double newX = target.X;
            double newY = target.Y;
            if (ignoreGoStraightPoint)
            {
                newX = target.X + GetWizardMaxStrafeSpeed(target) * time * Math.Cos(target.Angle + Math.PI / 2);
                newY = target.Y + GetWizardMaxStrafeSpeed(target) * time * Math.Sin(target.Angle + Math.PI / 2);
            }
            else
            {
                for (int i = 0; i <= time; ++i)
                {
                    newX = target.X + GetWizardMaxStrafeSpeed(target) * i * Math.Cos(target.Angle + Math.PI / 2);
                    newY = target.Y + GetWizardMaxStrafeSpeed(target) * i * Math.Sin(target.Angle + Math.PI / 2);

                    if (GetGoStraightPoint(newX, newY, 0) == null) break;
                }
            }

            if (newX < target.Radius || newY < target.Radius || newX > _world.Width - target.Radius ||
                  newY > _world.Height - target.Radius)
                return false;

            var isIntersect = Square.Intersect(
                  bsd.StartX,
                  bsd.StartY,
                  bsd.EndX,
                  bsd.EndY,
                  newX,
                  newY,
                  bsd.Radius,
                  target.Radius);

            return !isIntersect;
       
        }
        
        private LivingUnit GetNearestMyBaseAnemy(LaneType lane)
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

                if (unit is Minion && (unit as Minion).Faction == Faction.Neutral &&
                    (IsCalmNeutralMinion(unit as Minion) || !IsTotallyOnLine(unit, _line))) continue;

                if (!IsStrongOnLine(unit, lane)) continue;
                var dist = _selfBase.GetDistanceTo(unit);
                if (IsStrongOnLine(_self, lane) && _self.GetDistanceTo(_selfBase) - dist > _self.CastRange * 1.5) continue;

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

        private bool CanStayForBonus(double gotBonusTime)
        {
            var dangerousAnemies = GetDangerousAnemies(_self, _self.Radius * 2).Where(x => x is Wizard);
            var coolDownDamage = 0d;
            
            foreach (Wizard anemy in dangerousAnemies)
            {
                if (_self.Life > anemy.Life) return true;

                var nearestStaffRangeTarget = GetNearestStaffRangeTarget(anemy);

                var shootingDamage = GetShootingPower(anemy);
                var staffDamage = 0d;
                if (nearestStaffRangeTarget != null && nearestStaffRangeTarget.Id == _self.Id)
                {
                    staffDamage = GetStaffPower(anemy);
                }

                coolDownDamage += (shootingDamage + staffDamage);
            }

            var twoCooldownHp = coolDownDamage*2;

            var timeToTwoCooldownHp = (_self.Life - twoCooldownHp)/coolDownDamage*_game.MagicMissileCooldownTicks;

            return timeToTwoCooldownHp > _game.BonusAppearanceIntervalTicks - gotBonusTime;
        }

        private GoBonusResult CheckAndGoForBonus(LivingUnit nearestStaffRangeTarget, LivingUnit shootingTarget)
        {
           var goBonusResult = new GoBonusResult()
            {
                IsGo = false,
                GoToResult = null
            };
            var bonusAddTime = BONUS_ADD_TIME;

            //если игра 1 на 1
            if (_isOneOneOne)
            {
                return goBonusResult;
                
                if (GetMyLineType(_line) == LineType.Defensive || _myWizards[_line].Count - _anemyWizards[_line].Count >= 1) return goBonusResult;
                var ordered0Wizards =
                    _world.Wizards.Where(x => x.Faction == _self.Faction).OrderBy(x => _bonusPoints[0].getDistanceTo(x));

                var isNearest0 = false;
                foreach (var wizard in ordered0Wizards)
                {
                    if (wizard.IsMe)
                    {
                        isNearest0 = true;
                        break;
                    }
                    else
                    {
                        LaneType? wizardLaneType = null;
                        foreach (var lane in _myWizards.Keys)
                        {
                            if (_myWizards[lane].Contains(wizard.Id))
                            {
                                wizardLaneType = lane;
                                break;
                            }
                        }
                       
                        if (wizardLaneType != null && GetMyLineType(wizardLaneType.Value) != LineType.Defensive)
                        {
                            var myWizardsCount = _myWizards[wizardLaneType.Value].Count;
                            if (_line == wizardLaneType.Value) myWizardsCount++;
                            if (myWizardsCount - _anemyWizards[wizardLaneType.Value].Count <= 1) break;
                        }
                    }
                }

                var ordered1Wizards =
                   _world.Wizards.Where(x => x.Faction == _self.Faction).OrderBy(x => _bonusPoints[1].getDistanceTo(x));

                var isNearest1 = false;
                foreach (var wizard in ordered1Wizards)
                {
                    if (wizard.IsMe)
                    {
                        isNearest1 = true;
                        break;
                    }
                    else
                    {
                        LaneType? wizardLaneType = null;
                        foreach (var lane in _myWizards.Keys)
                        {
                            if (_myWizards[lane].Contains(wizard.Id))
                            {
                                wizardLaneType = lane;
                                break;
                            }
                        }
                        if (wizardLaneType != null && GetMyLineType(wizardLaneType.Value) != LineType.Defensive &&
                            _myWizards[wizardLaneType.Value].Count - _anemyWizards[wizardLaneType.Value].Count <= 1)
                        {
                            break;
                        }
                    }
                }

                bonusAddTime = BONUS_ADD_TIME_ONE_ON_ONE;
                if (!isNearest0 && !isNearest1) return goBonusResult;
            }
            
            //не идем, если атакуем дохлого волшебника
            if (IsOkToRunForWeakWizard(shootingTarget))
            {
                SetBonusZoneNotPasseble(_bonusPoints[0]);
                SetBonusZoneNotPasseble(_bonusPoints[1]);
                return goBonusResult;
            }

            //не идем, если берсерк-мод
            if (IsOkToRunForWeakWizard(shootingTarget))
            {
                return goBonusResult;
            }
            //если близко к чужой базе
            if (IsCloseToWin()) return goBonusResult;
            //Если можем сломать башню
            if (CanDestroyTower()) return goBonusResult;

            //не идем, если атакуем дохлую башню
            var nearestStaffRangeTargetBuilding = nearestStaffRangeTarget as Building;
            var shootingTargetBuilding = shootingTarget as Building;
            if (nearestStaffRangeTargetBuilding != null &&
                nearestStaffRangeTargetBuilding.Life <= nearestStaffRangeTargetBuilding.MaxLife * 0.5 ||
                shootingTargetBuilding != null && shootingTargetBuilding.Life <= shootingTargetBuilding.MaxLife * 0.5)
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

            var isMoreFriends = friends.Count > dangerousAnemies.Count();
            var isMoreAnemies = dangerousAnemies.Count() > friends.Count;
            if (isMoreAnemies ||
                _self.Life < _self.MaxLife * LOW_HP_FACTOR && IsInDangerousArea(_self, eps))
            {
                return goBonusResult;
            }

            var maxLength = 17 * _self.Radius * 2;
            var pathMaxLength = maxLength / _squareSize;
            
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
                dt0 = _game.BonusAppearanceIntervalTicks - (_gotBonus0Time + path0Time + bonusAddTime);
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
                dt1 = _game.BonusAppearanceIntervalTicks - (_gotBonus1Time + path1Time + bonusAddTime);
                if (path1.Count > pathMaxLength || dt1 > 0) path1 = null;

            }

            if (path0 != null && path1 != null)
            {
                if (path0Weight < path1Weight)
                {
                    if (isMoreFriends || CanStayForBonus(_gotBonus0Time))
                    {
                        var goToResult = GoToBonus(_bonusPoints[0], relaxCoeff, relax0Coeff, path0, _gotBonus0Time);
                        goBonusResult.IsGo = true;
                        goBonusResult.GoToResult = goToResult;
                    }
                }
                else
                {
                    if (isMoreFriends || CanStayForBonus(_gotBonus1Time))
                    {
                        var goToResult = GoToBonus(_bonusPoints[1], relaxCoeff, relax1Coeff, path1, _gotBonus1Time);
                        goBonusResult.IsGo = true;
                        goBonusResult.GoToResult = goToResult;
                    }
                }
                
            }
            else if (path0 != null && dt1 > path0Time)
            {
                if (isMoreFriends || CanStayForBonus(_gotBonus0Time))
                {
                    var goToResult = GoToBonus(_bonusPoints[0], relaxCoeff, relax0Coeff, path0, _gotBonus0Time);
                    goBonusResult.IsGo = true;
                    goBonusResult.GoToResult = goToResult;
                }

            }
            else if (path1 != null && dt0 > path1Time)
            {
                if (isMoreFriends || CanStayForBonus(_gotBonus1Time))
                {
                    var goToResult = GoToBonus(_bonusPoints[1], relaxCoeff, relax1Coeff, path1, _gotBonus1Time);
                    goBonusResult.IsGo = true;
                    goBonusResult.GoToResult = goToResult;
                }
            }
            return goBonusResult;

        }
        private GoToResult GoToBonus(Point2D bonusPoint, double relaxCoeff, double realRelaxCoeff, IList<Point> path, double gotBonusTime)
        {
            #region Эксперимент с боковыми точками - провалился
            var angle = Math.PI/2 - _self.Angle;
            var x1 = bonusPoint.X + _game.BonusRadius * Math.Cos(angle);
            var y1 = bonusPoint.X - _game.BonusRadius * Math.Sin(angle);

            var x2 = bonusPoint.X - _game.BonusRadius * Math.Cos(angle);
            var y2 = bonusPoint.X + _game.BonusRadius * Math.Sin(angle);
            #endregion

            GoToResult goToResult;
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
                        var strPoint = GetGoStraightPoint(wizardCp.X, wizardCp.Y, 0);
                        if (strPoint != null)
                        {
                            var selfTime = _self.GetDistanceTo(wizardCp.X, wizardCp.Y) / GetWizardMaxForwardSpeed(_self);
                            var nearstWizardTime = nearstWizard.GetDistanceTo(wizardCp.X, wizardCp.Y) /
                                                   GetWizardMaxForwardSpeed(nearstWizard);
                            var delta = 2;

                            if (selfTime < nearstWizardTime - delta &&
                                selfTime < (_game.BonusAppearanceIntervalTicks - gotBonusTime) - delta)
                            {
                                wizardPreventPoint = GetGoStraightPoint(wizardCp.X, wizardCp.Y, 0);
                            }

                        }

                    }
                }

                if (realRelaxCoeff == 0)
                {
                    _thisTickResPoint = bonusPoint;
                    goToResult = GoTo(
                         bonusPoint,
                        relaxCoeff - TOLERANCE * 100,
                        relaxCoeff - TOLERANCE * 100,
                        path);
                }
                else if (wizardPreventPoint != null)
                {
                    _thisTickResPoint = wizardPreventPoint;
                    goToResult = GoTo(wizardPreventPoint, 0, 0);
                }
                else
                {
                    _thisTickResPoint = bonusPoint;
                    goToResult = GoTo(
                        bonusPoint,
                        relaxCoeff,
                        relaxCoeff,
                        path);
                }
            }
            else
            {
                if (realRelaxCoeff == 0 || bonusPoint.getDistanceTo(_self) > _self.CastRange / 2)
                {
                    _thisTickResPoint = bonusPoint;
                    goToResult = GoTo(bonusPoint, relaxCoeff - TOLERANCE * 100, relaxCoeff - TOLERANCE * 100, path);
                }
                else
                {
                    goToResult = MakeBonusPath(bonusPoint);
                }
            }

            return goToResult;
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


        private bool CanNotRunBack(Wizard source, Wizard target)
        {
            var time =
                Math.Max(
                    source.RemainingActionCooldownTicks,
                    source.RemainingCooldownTicksByAction[(int) ActionType.MagicMissile]);
            time -= 2;

            var newX = target.X + GetWizardMaxBackSpeed(target) * time * Math.Cos(target.Angle - Math.PI);
            var newY = target.Y + GetWizardMaxBackSpeed(target) * time * Math.Sin(target.Angle - Math.PI);

            var newTarget = new Wizard(
                target.Id,
                newX,
                newY,
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

            var canShootWithMissle = CanShootWizardWithMissleNoCooldown(source, source.X, source.Y, newTarget, 0, true, false, false);

            var canShootWhithFireball = false;
            if (source.Skills.Any(x => x == SkillType.Fireball))
            {
                time =
                    Math.Max(
                        source.RemainingActionCooldownTicks,
                        source.RemainingCooldownTicksByAction[(int) ActionType.Fireball]);

                newX = target.X + GetWizardMaxBackSpeed(target)*time*Math.Cos(target.Angle - Math.PI);
                newY = target.Y + GetWizardMaxBackSpeed(target)*time*Math.Sin(target.Angle - Math.PI);

                newTarget = new Wizard(
                    target.Id,
                    newX,
                    newY,
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

                canShootWhithFireball = CanShootWizardWithFireballNoCooldown(source, source.X, source.Y, newTarget, 0, true, false, false);
            }

            var canShootWhithFrostbolt = false;
            if (source.Skills.Any(x => x == SkillType.FrostBolt))
            {
                time =
                    Math.Max(
                        source.RemainingActionCooldownTicks,
                        source.RemainingCooldownTicksByAction[(int) ActionType.FrostBolt]);

                newX = target.X + GetWizardMaxBackSpeed(target)*time*Math.Cos(target.Angle - Math.PI);
                newY = target.Y + GetWizardMaxBackSpeed(target)*time*Math.Sin(target.Angle - Math.PI);

                newTarget = new Wizard(
                    target.Id,
                    newX,
                    newY,
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

                canShootWhithFrostbolt = CanShootWizardWithFrostboltNoCooldown(source, source.X, source.Y, newTarget, 0, true, false, false);
            }

            return canShootWithMissle || canShootWhithFireball || canShootWhithFrostbolt;

        }
        

        private bool CanShootWizard(Wizard source, Wizard target, bool considerCooldown, bool checkOtherSides, bool addTick)
        {

            var missleCooldown = considerCooldown
                ? Math.Max(
                    source.RemainingActionCooldownTicks,
                    source.RemainingCooldownTicksByAction[(int) ActionType.MagicMissile])
                : 0;
            var canShootWithMissle = CanShootWizardWithMissleNoCooldown(
                source,
                source.X,
                source.Y,
                target,
                missleCooldown,
                checkOtherSides,
                true,
                addTick);

            var fireballCooldown = considerCooldown
                ? Math.Max(
                    source.RemainingActionCooldownTicks,
                    source.RemainingCooldownTicksByAction[(int) ActionType.Fireball])
                : 0;
            var canShootWithFireball = source.Skills.Any(x => x == SkillType.Fireball) &&
                                       CanShootWizardWithFireballNoCooldown(
                                           source,
                                           source.X,
                                           source.Y,
                                           target,
                                           fireballCooldown, 
                                           checkOtherSides, 
                                           true,
                                           addTick);

            var frostbaltCooldown = considerCooldown
                ? Math.Max(
                    source.RemainingActionCooldownTicks,
                    source.RemainingCooldownTicksByAction[(int) ActionType.Fireball])
                : 0;
            var canShootWithFrostBolt = source.Skills.Any(x => x == SkillType.FrostBolt) &&
                                        CanShootWizardWithFrostboltNoCooldown(
                                            source,
                                            source.X,
                                            source.Y,
                                            target,
                                            frostbaltCooldown,
                                            checkOtherSides,
                                            true,
                                            addTick);

            return canShootWithMissle || canShootWithFrostBolt || canShootWithFireball;
        }

        private bool NeedRunBack(LivingUnit source, Wizard target, IList<LivingUnit> friends, double selfNextTickX, double selfNextTickY)
        {
            var minion = source as Minion;
            if (minion != null)
            {
                if (_isOneOneOne &&
                    _self.GetDistanceTo(_anemyBaseX, _anemyBaseY) <= minion.GetDistanceTo(_anemyBaseX, _anemyBaseY))
                    return false;

                var attackRange = GetAttackRange(minion);
                var orderedFriends = friends.OrderBy(x => x.GetDistanceTo(source));
                var firstFriend = orderedFriends.First() as Wizard;
                var eps = _self.Radius/2;
                if (firstFriend != null && firstFriend.IsMe && _self.GetDistanceTo(minion) <= attackRange + eps)
                    return true;
            }

            var wizard = source as Wizard;
            if (wizard != null)
            {
                if (_isOneOneOne) return false;

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

                if (Math.Max(
                    wizard.RemainingActionCooldownTicks,
                    wizard.RemainingCooldownTicksByAction[(int) ActionType.MagicMissile]) <
                    Math.Max(
                        _self.RemainingActionCooldownTicks,
                        _self.RemainingCooldownTicksByAction[(int) ActionType.MagicMissile]))
                {
                    if (!friends.Any(x => x.Id != _self.Id && IsOkDistanceToShoot(wizard, x, 0d))) return true;
                }


                var newTarget = new Wizard(
                        target.Id,
                        selfNextTickX,
                        selfNextTickY,
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
                
                var startX = wizard.X + GetWizardMaxForwardSpeed(wizard)*Math.Cos(wizard.Angle);
                var startY = wizard.Y + GetWizardMaxForwardSpeed(wizard) * Math.Sin(wizard.Angle);

                //if (CanShootWizard(wizard, _self, false, true, false)) return true;
                //if (IsOkToRunForWizard(wizard, _self, false)) return true;
                if (CanNotRunBack(wizard, _self)) return true;
            }

            var building = source as Building;
            if (building != null)
            {
                if (_isOneOneOne)
                {
                    return building.Type != BuildingType.FactionBase && _self.Life > building.Damage && _self.Life < 100 &&
                           !CanGoToBuilding(building, friends) && building.X < 3800 && _world.TickIndex <= 1450 ||
                           building.Type == BuildingType.FactionBase && GetAliveAnemyTowers(_line).Count > 0 ;
                }
                //if (_isOneOneOne &&
                //    (building.Type == BuildingType.FactionBase ||
                //     _seenAnemyWizards.Count == 5 && (_myWizards[_line].Count - _anemyWizards[_line].Count >= 1)))
                //{
                //    //var isOkToGoOneOnOne = GetMyLineType(_line) == LineType.Agressive &&
                //    //                       _self.Life > _self.MaxLife*HP_FACTOR_TO_GO_TO_TOWERS;
                //    //return !isOkToGoOneOnOne;
                //    return false;
                //}

                //return false;
                return !CanGoToBuilding(building, friends);
            }

            return false;
        }

      
        private bool CanGoToBuilding (Building building, IList<LivingUnit> friends)
        {
            const int eps = 70;

            var nearFriends = friends.Where(f => building.GetDistanceTo(f) <= building.AttackRange && f.Id != _self.Id);

            var selfBuildingDist = _self.GetDistanceTo(building);
            var resultDist = selfBuildingDist + GetWizardMaxBackSpeed(_self) * building.RemainingActionCooldownTicks;
            var canGoBack = resultDist - eps > building.AttackRange;

            var friendsCount = building.Type == BuildingType.GuardianTower ? 2 : 3;
            if (_self.Life < building.Damage)
            {
                return nearFriends.Count(x => x.Life > _self.Life && !CanFriendGoBack(x, building)) >=2 || canGoBack;
            }
            else if (_self.Life > building.Damage)
            {
                if (nearFriends.Count(x => x.Life >= building.Damage && x.Life < _self.Life && !CanFriendGoBack(x, building)) >= 2) return true;
                var myHpFriends = nearFriends.Where(x => x.Life == _self.Life);
               
                return myHpFriends.Count() >= friendsCount || canGoBack;
            }
            
            //здоровье равно атаке башни
            return canGoBack;
        }

        private bool CanFriendGoBack(LivingUnit unit, Building building)
        {
            var wizard = unit as Wizard;
            if (wizard == null) return false;

            var friendBuildingDist = wizard.GetDistanceTo(building);
            var resultFriendDist = friendBuildingDist + GetWizardMaxBackSpeed(wizard) * building.RemainingActionCooldownTicks;
            return resultFriendDist > building.AttackRange;
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
        private bool CanShootWithMissle(Wizard source, double startX, double startY, Wizard target, double turnTime, 
            bool checkBack, bool checkForward, bool checkSide, bool considerCooldown)
        {

            var bsd = new BulletStartData(
                startX,
                startY,
                source.CastRange,
                target.X - startX,
                target.Y - startY,
                _game.MagicMissileRadius,
                _game.MagicMissileSpeed);


            var bulletTime = GetBulletTime(bsd, target);
            var goAwayTime = bulletTime;
            if (considerCooldown)
                goAwayTime += Math.Max(
                    source.RemainingActionCooldownTicks,
                    source.RemainingCooldownTicksByAction[(int) ActionType.MagicMissile]);

            var canGoBack = checkBack && CanGoBack(target, bsd, goAwayTime, true);
            var canGoForward = checkForward && CanGoForward(target, bsd, goAwayTime, true);
            var canGoLeft = checkSide && CanGoLeft(target, bsd, goAwayTime, true);
            var canGoRight = checkSide && CanGoRight(target, bsd, goAwayTime, true);

            return !canGoBack && !canGoForward && !canGoLeft && !canGoRight;
        }

        private bool CanShootWizardWithMissleNoCooldown(
            Wizard source,
            double sourceX,
            double sourceY,
            Wizard target,
            int sourceCooldown,
            bool checkOtherSides,
            bool considerTurnTime,
            bool addTick)
        {
            var bsd = new BulletStartData(
                sourceX,
                sourceY,
                source.CastRange,
                target.X - sourceX,
                target.Y - sourceY,
                _game.MagicMissileRadius,
                _game.MagicMissileSpeed);

            var angle = source.GetAngleTo(target);
            var deltaAngle = Math.Abs(angle) - _game.StaffSector / 2;
            int turnTime = 0;

            if (considerTurnTime)
            {
                if (deltaAngle <= 0)
                {
                    turnTime = 0;
                }
                else
                {
                    turnTime = (int) (deltaAngle/GetWizardMaxTurn(source)) + 1;
                }
            }

            var bulletTime = GetBulletTime(bsd, target);
            bulletTime += Math.Max(sourceCooldown, turnTime);
            //if (addTick) bulletTime++; //+1, т.к. может уйти на этом ходу

            var canGoBack = CanGoBack(target, bsd, bulletTime, true);
            if (!checkOtherSides)
            {
                return !canGoBack;
            }

            var canGoForward = CanGoForward(target, bsd, bulletTime, true);
            var canGoLeft = CanGoLeft(target, bsd, bulletTime, true);
            var canGoRight = CanGoRight(target, bsd, bulletTime, true);

            return !canGoBack && !canGoForward && !canGoLeft && !canGoRight;
        }

        private double GetTurnTime(Wizard source, LivingUnit target)
        {
            var angle = source.GetAngleTo(target);
            var deltaAngle = Math.Abs(angle) - _game.StaffSector / 2;
            int turnTime = 0;
           
            if (deltaAngle <= 0)
            {
                turnTime = 0;
            }
            else
            {
                turnTime = (int)(deltaAngle / GetWizardMaxTurn(source)) + 1;
            }

            return turnTime;
            
        }

        private bool CanShootWizardWithFrostboltNoCooldown(Wizard source, double sourceX, double sourceY, Wizard target, int sourceCooldown, bool checkOtherSides, bool considerTurnTime, bool addTick)
        {
            var bsd = new BulletStartData(
               sourceX,
               sourceY,
                source.CastRange,
               target.X - sourceX,
               target.Y - sourceY,
               _game.FrostBoltRadius,
               _game.FrostBoltSpeed);

            var angle = source.GetAngleTo(target);
            var deltaAngle = Math.Abs(angle) - _game.StaffSector / 2;
            int turnTime=0;
            if (considerTurnTime)
            {
                if (deltaAngle <= 0)
                {
                    turnTime = 0;
                }
                else
                {
                    turnTime = (int) (deltaAngle/GetWizardMaxTurn(source)) + 1;
                }
            }

            var bulletTime = GetBulletTime(bsd, target);
            bulletTime += Math.Max(sourceCooldown, turnTime);
            //if (addTick) bulletTime++; //+1, т.к. может уйти на этом ходу

            var canGoBack = CanGoBack(target, bsd, bulletTime, true);
            if (!checkOtherSides)
            {
                return !canGoBack;
            }

            var canGoForward = CanGoForward(target, bsd, bulletTime, true);
            var canGoLeft = CanGoLeft(target, bsd, bulletTime, true);
            var canGoRight = CanGoRight(target, bsd, bulletTime, true);

            return !canGoBack && !canGoForward && !canGoLeft && !canGoRight;
        }

        private bool CanShootWizardWithFireballNoCooldown(Wizard source, double sourceX, double sourceY, Wizard target, int sourceCooldown, bool checkOtherSides, bool considerTurnTime, bool addTick)
        {
            var bsd = new BulletStartData(
               sourceX,
               sourceY,
               source.CastRange,
               target.X - sourceX,
               target.Y - sourceY,
               _game.FireballRadius, 
               _game.FireballSpeed);

            var angle = source.GetAngleTo(target);
            var deltaAngle = Math.Abs(angle) - _game.StaffSector / 2;
            int turnTime=0;
            if (considerTurnTime)
            {
                if (deltaAngle <= 0)
                {
                    turnTime = 0;
                }
                else
                {
                    turnTime = (int) (deltaAngle/GetWizardMaxTurn(source)) + 1;
                }
            }

            var bulletTime = GetBulletTime(bsd, target);
            bulletTime += Math.Max(sourceCooldown, turnTime);
            //if (addTick) bulletTime++; //+1, т.к. может уйти на этом ходу

            var canGoBack = CanGoBack(target, bsd, bulletTime, true);
            if (!checkOtherSides)
            {
                return !canGoBack;
            }
            var canGoForward = CanGoForward(target, bsd, bulletTime, true);
            var canGoLeft = CanGoLeft(target, bsd, bulletTime, true);
            var canGoRight = CanGoRight(target, bsd, bulletTime, true);

            return !canGoBack && !canGoForward && !canGoLeft && !canGoRight;
        }

  
        private int GetBulletTime(BulletStartData bulletStartData, Wizard target)
        {
            Point2D bulletPoint;
            if (bulletStartData.Bullet != null)
            {
                bulletPoint = new Point2D(bulletStartData.Bullet.X, bulletStartData.Bullet.Y);
            }
            else
            {
                bulletPoint = new Point2D(bulletStartData.StartX, bulletStartData.StartY);
            }
            var dist = bulletPoint.getDistanceTo(target) - target.Radius;
            var time = dist/bulletStartData.Speed;
            return (int) time + 1;

            //Point2D currBulletPoint;
            //int t;
            //double bulletAngle;
            //var currTargetPoint = new Point2D(target.X, target.Y);

            //if (bulletStartData.Bullet != null)
            //{
            //    currBulletPoint = new Point2D(bulletStartData.Bullet.X, bulletStartData.Bullet.Y);
            //    t = 0;
            //    bulletAngle = bulletStartData.Bullet.Angle;
            //}
            //else
            //{
            //    currBulletPoint = new Point2D(bulletStartData.StartX, bulletStartData.StartY);
            //    t = 0;
            //    bulletAngle = bulletStartData.Angle;
            //}


            //var isIn = currTargetPoint.getDistanceTo(currBulletPoint) < target.Radius + bulletStartData.Radius ||
            //           currBulletPoint.getDistanceTo(bulletStartData.StartX, bulletStartData.StartY) >
            //           currTargetPoint.getDistanceTo(bulletStartData.StartX, bulletStartData.StartY);
            //while (!isIn)
            //{
            //    t++;

            //    var newTargetX = currTargetPoint.X + GetWizardMaxBackSpeed(target) * Math.Cos(target.Angle - Math.PI);
            //    var newTargetY = currTargetPoint.Y + GetWizardMaxBackSpeed(target) * Math.Sin(target.Angle - Math.PI);
            //    currTargetPoint = new Point2D(newTargetX, newTargetY);

            //    var newBulletX = currBulletPoint.X + bulletStartData.Speed * Math.Cos(bulletAngle);
            //    var newBulletY = currBulletPoint.Y + bulletStartData.Speed * Math.Sin(bulletAngle);
            //    currBulletPoint = new Point2D(newBulletX, newBulletY);

            //    isIn = currTargetPoint.getDistanceTo(currBulletPoint) < target.Radius + bulletStartData.Radius ||
            //          currBulletPoint.getDistanceTo(bulletStartData.StartX, bulletStartData.StartY) >
            //           currTargetPoint.getDistanceTo(bulletStartData.StartX, bulletStartData.StartY); ;
            //}
            //return t;
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

        private void SetBonusZoneNotPasseble(Point2D bonusPoint)
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

        }

        private GoToResult MakeBonusPath(Point2D bonusPoint)
        {
            SetBonusZoneNotPasseble(bonusPoint);

            var resPoint = new Point2D(2 * bonusPoint.X - _self.X, 2 * bonusPoint.Y - _self.Y);

            _thisTickResPoint = resPoint;
            return GoTo(resPoint, _self.Radius * 2, _self.Radius * 2);
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
            if (_isOneOneOne) return false;
            return _self.Life < _self.MaxLife * LOW_HP_FACTOR && IsInDangerousArea(_self, _self.Radius * 2);
        }

        private bool IsPointVisible(double x, double y, double eps)
        {
            foreach (var unit in _world.Buildings.Where(u => u.Faction == _self.Faction))
            {
                var dist = unit.GetDistanceTo(x, y);
                if (dist + eps <= unit.VisionRange)
                {
                    return true;
                }
            }
            foreach (var unit in _world.Wizards.Where(u => u.Faction == _self.Faction))
            {
                var dist = unit.GetDistanceTo(x, y);
                if (dist + eps <= unit.VisionRange)
                {
                    return true;
                }
            }
            foreach (var unit in _world.Minions.Where(u => u.Faction == _self.Faction))
            {
                var dist = unit.GetDistanceTo(x, y);
                if (dist + eps <= unit.VisionRange)
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

            //var anemies = new List<LivingUnit>();
            //anemies.AddRange(_world.Wizards.Where(x => x.Faction != _self.Faction));
            //anemies.AddRange(_world.Minions.Where(x => x.Faction != _self.Faction && (!IsCalmNeutralMinion(x) || x.Faction != Faction.Neutral)));
            //for (int i = 0; i < _anemyBuildings.Count; ++i)
            //{
            //    if (!_IsAnemyBuildingAlive[i]) continue;
            //   anemies.Add(_anemyBuildings[i]);
            //}


            //foreach (var anemy in anemies)
            //{
            //    var attackRange = GetAttackRange(anemy);
            //    var weight = GetSquareWeight(anemy);
            //    var x1 = anemy.X - attackRange;
            //    var y1 = anemy.Y - attackRange;
            //    var x2 = anemy.X + attackRange;
            //    var y2 = anemy.Y + attackRange;

            //    if (_lastTickPath != null)
            //    {
            //        foreach (Square p in _lastTickPath)
            //        {
            //            //if (p.Weight > 1) continue;

            //            if (p.X + p.Side >= x1 && p.X <= x2 && p.Y + p.Side >= y1 && p.Y <= y2)
            //            {
            //                var centerX = p.X + _squareSize / 2;
            //                var centerY = p.Y + _squareSize / 2;
            //                var dist = anemy.GetDistanceTo(centerX, centerY);
            //                if (dist < attackRange)
            //                {
            //                    p.Weight += weight;
            //                }
            //            }
            //        }
            //    }

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
            //            //if (_table[i, j].Weight > 1) continue;

            //            var centerX = _startX + i * _squareSize + _squareSize / 2;
            //            var centerY = _startY + j * _squareSize + _squareSize / 2;
            //            var dist = anemy.GetDistanceTo(centerX, centerY);
            //            if (dist < attackRange)
            //            {
            //                _table[i, j].Weight += weight;
            //            }
            //        }
            //    }
            //}









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

                        if (p.X + p.Side >= x1 && p.X <= x2 && p.Y + p.Side >= y1 && p.Y <= y2)
                        {
                            var centerX = p.X + _squareSize / 2;
                            var centerY = p.Y + _squareSize / 2;
                            var dist = _anemyBuildings[k].GetDistanceTo(centerX, centerY);
                            if (dist < _anemyBuildings[k].AttackRange)
                            {
                                p.Weight += IsStrongOnLine(_anemyBuildings[k], _line) ||
                                            _anemyBuildings[k].Type == BuildingType.FactionBase
                                    ? _isOneOneOne ? 0d : LIGHT_SHOOTING_SQUARE_WEIGHT
                                    : STRONG_SHOOTING_SQUARE_WEIGHT;
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

                        var centerX = _startX + i * _squareSize + _squareSize / 2;
                        var centerY = _startY + j * _squareSize + _squareSize / 2;
                        var dist = _anemyBuildings[k].GetDistanceTo(centerX, centerY);
                        if (dist < _anemyBuildings[k].AttackRange)
                        {
                            _table[i, j].Weight += IsStrongOnLine(_anemyBuildings[k], _line) ||
                                                   _anemyBuildings[k].Type == BuildingType.FactionBase
                                ? _isOneOneOne ? 0d : LIGHT_SHOOTING_SQUARE_WEIGHT
                                : STRONG_SHOOTING_SQUARE_WEIGHT;
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

                        if (p.X + p.Side >= x1 && p.X <= x2 && p.Y + p.Side >= y1 && p.Y <= y2)
                        {
                            var centerX = p.X + _squareSize / 2;
                            var centerY = p.Y + _squareSize / 2;
                            var dist = w.GetDistanceTo(centerX, centerY);
                            if (dist < w.CastRange)
                            {
                                p.Weight += LIGHT_SHOOTING_SQUARE_WEIGHT;
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

                        var centerX = _startX + i * _squareSize + _squareSize / 2;
                        var centerY = _startY + j * _squareSize + _squareSize / 2;
                        var dist = w.GetDistanceTo(centerX, centerY);
                        if (dist < w.CastRange)
                        {
                            _table[i, j].Weight += LIGHT_SHOOTING_SQUARE_WEIGHT;
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

            //if (_isLineSet)
            //{
            //    for (int i = 0; i < _n; ++i)
            //    {
            //        for (int j = 0; j < _m; ++j)
            //        {
            //            var square = _table[i, j];

            //            if (square.X <= ROW_WIDTH*2 && square.Y >= _world.Height - ROW_WIDTH*2) continue;
            //            if (square.Y <= ROW_WIDTH*2 && square.X >= _world.Width - ROW_WIDTH*2) continue;

            //            if (_line == LaneType.Top && square.Y > _world.Width - ROW_WIDTH - square.X)
            //            {
            //                square.Weight = 999999;
            //            }
            //            if (_line == LaneType.Bottom && square.Y < _world.Width + ROW_WIDTH - square.X)
            //            {
            //                square.Weight = 999999;
            //            }
            //            if (_line == LaneType.Middle && (square.X <= ROW_WIDTH || square.Y <= ROW_WIDTH ||
            //                                             square.X >= _world.Width - ROW_WIDTH ||
            //                                             square.Y >= _world.Height - ROW_WIDTH))
            //            {
            //                square.Weight = 999999;
            //            }
            //        }
            //    }
            //}
        }
        
        private Point2D GetRadiusPoint(double borderDist, double radius, LivingUnit target)
        {
            if (_isOneOneOne) return null;

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
                if (anemy is Minion && (anemy as Minion).Faction == Faction.Neutral && IsCalmNeutralMinion(anemy as Minion)) continue;

                var correctEps = eps;
                if (anemy is Wizard && (anemy as Wizard).Skills.Any(x => x == SkillType.Fireball))
                    correctEps = eps + _self.Radius*2;

                var canShoot = IsOkDistanceToShoot(anemy, unit, correctEps);
                if (canShoot) result.Add(anemy);

            }
            return result;
        }

        private bool IsInDangerousArea(LivingUnit unit, double eps)
        {
            return GetDangerousAnemies(unit, eps).Any();
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

        private GoToResult GoBack()
        {
            var beforePrevWaypoint = GetBeforePreviousWaypoint();
            _thisTickResPoint = beforePrevWaypoint;
            return GoTo(beforePrevWaypoint, _self.Radius * 4, 0);
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
        
        private LivingUnit GetNearestStaffRangeTarget(Wizard source)
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
                if (target.Faction == source.Faction)
                {
                    continue;
                }


                if (target is Minion && (target as Minion).Faction == Faction.Neutral && IsCalmNeutralMinion(target as Minion)) continue;

                //var angle = _self.GetAngleTo(target);
                //if (Math.Abs(angle) > _game.StaffSector / 2.0D) continue;

                var distance = source.GetDistanceTo(target) - target.Radius;
                if (distance > _game.StaffRange) continue;

                if (target is Building && (target as Building).Type == BuildingType.FactionBase) return target;

                if (source.GetDistanceTo(target) <= minDist)
                {
                    if (source.GetDistanceTo(target) < minDist || Math.Abs(source.GetAngleTo(target)) < minAngle)
                    {
                        nearestTarget = target;
                        minDist = source.GetDistanceTo(target);
                        minAngle = Math.Abs(source.GetAngleTo(target));
                    }
                }
            }

            return nearestTarget;
        }
        
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
     
        private void InitializeStrategy(Wizard self, Game game)
        {
            if (_random == null)
            {
                _random = new Random(DateTime.Now.Millisecond);
                
                if (_world.Players[0].Name == _world.Players[1].Name && _world.Players[0].Name == _world.Players[2].Name &&
                    _world.Players[0].Name == _world.Players[3].Name && _world.Players[0].Name == _world.Players[4].Name)
                {
                    _isOneOneOne = true;
                }

                //if (_isOneOneOne &&
                //    _world.Players.Any(
                //        x =>
                //            x.Name == "Romka" || x.Name == "jetblack" || x.Name == "mustang" || x.Name == "Antmsu" ||
                //            x.Name == "tyamgin" ||
                //            x.Name == "core2duo" ||
                //            x.Name == "OrickBy" || 
                //            x.Name == "dedoo" || 
                //            x.Name == "byserge" || 
                //            x.Name == "ud1" || 
                //            x.Name == "NighTurs" || 
                //            x.Name == "WildCat" || 
                //            x.Name == "cheeser" || 
                //            x.Name == "Oxidize" || 
                //            x.Name == "Commandos" ))
                //{
                //    _isCheatingStrategy = true;
                //}

                _bulletStartDatas = new Dictionary<long, BulletStartData>();

                _myWizards = new Dictionary<LaneType, IList<long>>()
                {
                    {LaneType.Top, new List<long>()},
                    {LaneType.Middle, new List<long>()},
                    {LaneType.Bottom, new List<long>()},
                };

                _anemyWizards = new Dictionary<LaneType, IList<long>>()
                {
                    {LaneType.Top, new List<long>()},
                    {LaneType.Middle, new List<long>()},
                    {LaneType.Bottom, new List<long>()},
                };

                _allMyWizards = new List<Wizard>();
                _allAnemyWizards = new List<Wizard>();
                _allMyWizards.AddRange(_world.Wizards.Where(x => !x.IsMe && x.Faction == _self.Faction));

                _seenAnemyWizards = new List<Wizard>();

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

                _cheatingWaypointsByLine = new Point2D[]
                {
                    new Point2D(100.0D, mapSize - 100.0D),
                    new Point2D(400.0D, mapSize - 100.0D),
                    new Point2D(1800.0D, mapSize - 200.0D),
                    new Point2D(mapSize - 200.0D, 2000),
                    new Point2D(mapSize - 200.0D, 1600D),
                    new Point2D(mapSize - 200.0D, 200.0D),


                };
                //if (_isOneOneOne && (_self.Id % 5 == 1 || _self.Id % 5 == 2)) _line = LaneType.Top;
                //else _line = LaneType.Middle;

                //_line = LaneType.Top;
                //if (_isOneOneOne && (_self.Id % 5 == 1 || _self.Id % 5 == 2) && _isCheatingStrategy) _line = LaneType.Top;
                //else _line = LaneType.Middle;

            if (_isOneOneOne) _line = LaneType.Bottom;
            else _line = LaneType.Middle;
                


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
        private void InitializeTick(Wizard self, World world, Game game, Move move)
        {
            _self = self;
            _world = world;
            _game = game;
            _move = move;
        }
     
        private Point2D GetNextWaypoint()
        {

            

            Point2D[] line = _isOneOneOne ? _cheatingWaypointsByLine : _waypointsByLine[_line];

            int lastWaypointIndex = line.Length - 1;
            Point2D lastWaypoint = line[lastWaypointIndex];

            for (int waypointIndex = 0; waypointIndex < lastWaypointIndex; ++waypointIndex)
            {
                Point2D waypoint = line[waypointIndex];

                if (waypoint.getDistanceTo(_self) <= WAYPOINT_RADIUS)
                {
                    return line[waypointIndex + 1];
                }

                if (lastWaypoint.getDistanceTo(waypoint) < lastWaypoint.getDistanceTo(_self))
                {
                    return waypoint;
                }
            }

            return lastWaypoint;
        }
    
        private Point2D GetBeforePreviousWaypoint()
        {
            Point2D[] line = _isOneOneOne ? _cheatingWaypointsByLine : _waypointsByLine[_line];

            Point2D firstWaypoint = line[0];

            for (int waypointIndex = line.Length - 1; waypointIndex > 0; --waypointIndex)
            {
                Point2D waypoint = line[waypointIndex];

                if (waypoint.getDistanceTo(_self) <= WAYPOINT_RADIUS)
                {
                    return line[waypointIndex - 1];
                }

                if (firstWaypoint.getDistanceTo(waypoint) < firstWaypoint.getDistanceTo(_self))
                {
                    return line[waypointIndex - 1];
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

        private bool ShouldAttackNeutralMinion(Minion minion)
        {
            var units = new List<LivingUnit>();
            units.AddRange(_world.Wizards);
            units.AddRange(_world.Minions.Where(x => x.Faction != Faction.Neutral));
            units.AddRange(_world.Buildings.Where(x => x.Faction == _self.Faction));
          

            for (int i = 0; i < _anemyBuildings.Count; ++i)
            {
                if (!_IsAnemyBuildingAlive[i]) continue;
                units.Add(_anemyBuildings[i]);
            }

            var sortedUnits = units.OrderBy(x => x.GetDistanceTo(minion));
            var nearestUnit = sortedUnits.FirstOrDefault();

            var isCalm = IsCalmNeutralMinion(minion);
            if (isCalm)
            {
                //стреляем, если ближе враг
                var neutrals = _world.Minions.Where(x => x.Faction == Faction.Neutral && x.GetDistanceTo(minion) < 500);
                var friends = new List<LivingUnit>();
                var anemies = new List<LivingUnit>();
                foreach (var neutral in neutrals)
                {
                    var newSortedUnits = units.OrderBy(x => x.GetDistanceTo(neutral));
                    var newNearestUnit = newSortedUnits.First();
                    //if (neutral.GetDistanceTo(newNearestUnit) > 500) continue;
                    if (newNearestUnit.Faction == _self.Faction)
                    {
                        friends.Add(newNearestUnit);
                    }
                    else
                    {
                        anemies.Add(newNearestUnit);
                    }
                }

                return !friends.Any(x => x is Wizard) && anemies.Count > friends.Count;

            }
            else
            {
                //стреляем, если ближе наш союзник
                return nearestUnit != null && nearestUnit.Faction == _self.Faction;
            }
        }

        private bool IsCalmNeutralMinion(Minion minion)
        {
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
            var minTreeHp = double.MaxValue;
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
                    var hp = tree.Life;
                    if (hp < minTreeHp)
                    {
                        minTreeHp = hp;
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


        private GoToResult GoTo(Point2D point, double relaxCoeff, double strightRelaxCoeff, IList<Point> path = null)
        {
            if (!_isLineSet && _world.TickIndex < 600 && !_isOneOneOne)
            {
                point = new Point2D(900, _world.Height - 1000);
            }
            
            //SpeedContainer speedContainer = null;
            var goStraightPoint = GetGoStraightPoint(point.X, point.Y, strightRelaxCoeff);
            if (goStraightPoint != null)
            {
                //speedContainer = GetSpeedContainer(goStraightPoint.X, goStraightPoint.Y);
                //var isInPoint = Math.Abs(_self.X - point.X) < TOLERANCE &&
                //                Math.Abs(_self.Y - point.Y) < TOLERANCE;
                //if (needTurn && !isInPoint)
                //{
                //    _move.Turn = _self.GetAngleTo(point.X, point.Y);
                //}

                //_move.Speed = speedContainer.Speed;
                //_move.StrafeSpeed = speedContainer.StrafeSpeed;
                _lastTickPath = null;
                _lastTickResPoint = null;
                return new GoToResult()
                {
                    WoodCuTree = null,
                    X = goStraightPoint.X,
                    Y = goStraightPoint.Y,
                };
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

         
            if (path.Count <= 1)
            {
                resX = point.X;
                resY = point.Y;
            }
            else
            {
                resX = (path[1] as Square).X + _squareSize / 2;
                resY = (path[1] as Square).Y + _squareSize / 2;
            }

            Tree woodCutTree = null;
            if (path.Count > 1) woodCutTree = (Tree) GetWoodCutTree(path[0] as Square, path[1] as Square);

            //speedContainer = GetSpeedContainer(resX, resY);
            
            //_move.Speed = speedContainer.Speed;
            //_move.StrafeSpeed = speedContainer.StrafeSpeed;

            //LivingUnit woodCutTree = null;
            //if (path.Count > 1)
            //{
            //    woodCutTree = GetWoodCutTree(path[0] as Square, path[1] as Square);
            //    if (woodCutTree != null)
            //    {
            //        var angle = _self.GetAngleTo(woodCutTree);
            //        _move.Turn = angle;
            //        var distance = _self.GetDistanceTo(woodCutTree) - woodCutTree.Radius;

            //        if (Math.Abs(angle) <= _game.StaffSector / 2.0D)
            //        {
            //            if (_self.RemainingActionCooldownTicks == 0 && distance <= _game.StaffRange &&
            //                _self.RemainingCooldownTicksByAction[(int)ActionType.Staff] == 0)
            //            {
            //                _move.Action = ActionType.Staff;
            //            }
            //            else if (_self.RemainingActionCooldownTicks == 0 &&
            //                     _self.RemainingCooldownTicksByAction[(int)ActionType.MagicMissile] == 0)
            //            {
            //                _move.Action = ActionType.MagicMissile;
            //            }
            //        }

            //    }
            //}

            //if (needTurn && woodCutTree == null)
            //{
            //    _move.Turn = _self.GetAngleTo(resX, resY);
            //}

            #region Сохраняем путь для следующей итерации
            _lastTickPath = new List<Point>();
            foreach (Square p in path)
            {
                _lastTickPath.Add(new Square(p.Side, p.X, p.Y, 1d, p.Name, _game));
            }
            _lastTickResPoint = _thisTickResPoint;
            #endregion

            return new GoToResult()
            {
                WoodCuTree = woodCutTree,
                X = resX,
                Y = resY,
            };


        }
      

        private LivingUnit GetClosestTarget()
        {
            var targets = new List<LivingUnit>();
            targets.AddRange(_world.Buildings);
            targets.AddRange(_world.Wizards);
            targets.AddRange(_world.Minions);

            LivingUnit closestTarget = null;
            var minDist = double.MaxValue;

            foreach (var target in targets)
            {
                if (target.Faction == _self.Faction)
                {
                    continue;
                }

                if (target is Building && !IsStrongOnLine(target, _line)) continue;
                
                if (target is Minion && (target as Minion).Faction == Faction.Neutral && IsCalmNeutralMinion(target as Minion)) continue;

                var dist = _self.GetDistanceTo(target);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestTarget = target;
                }
            }
            return closestTarget;
        }

        private LineType GetMyLineType(LaneType laneType)
        {
            var myWizardsCount = _myWizards[laneType].Count;
            if (laneType == _line) myWizardsCount++;

            if (myWizardsCount > _anemyWizards[laneType].Count) return LineType.Agressive;
            if (_anemyWizards[laneType].Count == myWizardsCount) return LineType.Neutral;
            return LineType.Defensive;
        }

        private LivingUnit GetAgressiveLineShootingTarget()
        {
            _isBerserkTarget = false;
            LivingUnit shootingTarget = null;

            #region Берсерк
            
            var berserkTarget = GetBerserkTarget();
            if (berserkTarget != null && berserkTarget.MyWizards.Any(x => x.Id == _self.Id))
            {
                _isBerserkTarget = true;
                return berserkTarget.Target;
            }

            #endregion

            #region Дохлый волшебник

            var wizards = _world.Wizards.Where(x => x.Faction != _self.Faction);

            //LivingUnit possibleShootingTarget = null;

            var minHp = double.MaxValue;
            //var possibleMinHp = double.MaxValue;

            foreach (var target in wizards)
            {
                if (target.Faction == _self.Faction) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                var life = target.Life;
                var distanceToRunForWeak = _self.CastRange*COEFF_TO_RUN_FOR_WEAK; 

                if (IsWeakWizard(target) && _self.GetDistanceTo(target) <= distanceToRunForWeak)
                {
                    if (life < minHp)
                    {
                        minHp = life;
                        shootingTarget = target;
                    }
                }
            }

            if (shootingTarget != null) return shootingTarget;
            #endregion

            #region Спокойный нейтрал

            if (!IsCloseToWin() || GetLineAliveAnemyTowers(_line).Count > 0)
            {
                var neutrals = _world.Minions.Where(x => x.Faction == Faction.Neutral);
                minHp = double.MaxValue;
                foreach (var target in neutrals)
                {
                    if (!IsCalmNeutralMinion(target) || !ShouldAttackNeutralMinion(target)) continue;
                    if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                    if (IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                    var life = target.Life;
                    if (life < minHp)
                    {
                        minHp = life;
                        shootingTarget = target;
                    }
                }
            }

            if (shootingTarget != null) return shootingTarget;
            #endregion

            #region Обычная башня или дохлая база

            var minDist = double.MaxValue;
            foreach (var target in _world.Buildings)
            {
                if (target.Faction == _self.Faction) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;
                if(target.Type != BuildingType.FactionBase && !IsStrongOnLine(target, _line)) continue;

                double distance = _self.GetDistanceTo(target);

                if (distance < minDist && target.Type == BuildingType.GuardianTower || target.Life <= target.MaxLife * 0.5)
                {
                    minDist = distance;
                    shootingTarget = target;
                }
            }
            if (shootingTarget != null) return shootingTarget;

            #endregion
    
            #region Обычный волшебник

            //LivingUnit possibleShootingTarget = null;

            minHp = double.MaxValue;
            //var possibleMinHp = double.MaxValue;

            foreach (var target in wizards)
            {
                if (target.Faction == _self.Faction) continue;
                //if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                if (_self.GetDistanceTo(target) > _self.CastRange * 1.5) continue;
                if (!IsOkToRunForWizard(_self, target)) continue;
                

                //var canShootWizard = CanShootWizard(_self, target, true, true, true);

                var life = target.Life;

                //if (canShootWizard)
                //{
                    if (life < minHp)
                    {
                        minHp = life;
                        shootingTarget = target;
                    }
                //}
            }

            if (shootingTarget != null) return shootingTarget;
            #endregion

            #region Опасный миньон
            var minions = _world.Minions;
            minHp = double.MaxValue;
            foreach (var target in minions.Where(x => x.Type == MinionType.FetishBlowdart && x.GetDistanceTo(_self) < GetAttackRange(x)))
            {
                if (target.Faction == _self.Faction) continue;
                if (target.Faction == Faction.Neutral && !ShouldAttackNeutralMinion(target)) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                //double distance = _self.GetDistanceTo(target);

                var life = target.Life;
                if (life < minHp)
                {
                    minHp = life;
                    shootingTarget = target;
                }
            }

            if (shootingTarget != null) return shootingTarget;

            #endregion

            #region База
            minDist = double.MaxValue;
            foreach (var target in _world.Buildings)
            {
                if (target.Faction == _self.Faction) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;
                if (target.Type != BuildingType.FactionBase && !IsStrongOnLine(target, _line)) continue;

                double distance = _self.GetDistanceTo(target);

                if (distance < minDist && target.Life <= target.MaxLife)
                {
                    minDist = distance;
                    shootingTarget = target;
                }
            }
            if (shootingTarget != null) return shootingTarget;

            #endregion

            #region Миньон
            minHp = double.MaxValue;
            foreach (var target in minions)
            {
                if (target.Faction == _self.Faction) continue;
                if (target.Faction == Faction.Neutral && !ShouldAttackNeutralMinion(target)) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                //double distance = _self.GetDistanceTo(target);

                var life = target.Life;
                if (life < minHp)
                {
                    minHp = life;
                    shootingTarget = target;
                }
            }

            if (shootingTarget != null) return shootingTarget;

            #endregion

          
            return null;
        }

        private LivingUnit GetOneOnOneShootingTarget()
        {
            _isBerserkTarget = false;
            LivingUnit shootingTarget = null;
            var minDist = double.MaxValue;
            
            #region Здание

            minDist = double.MaxValue;
            foreach (
                var target in
                    _world.Buildings.Where(x => x.Type == BuildingType.FactionBase || _world.TickIndex <= 1450))
            {
                if (target.Faction == _self.Faction) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;
                if (target.Type != BuildingType.FactionBase && !IsStrongOnLine(target, _line)) continue;

                double distance = _self.GetDistanceTo(target);

                if (distance < minDist)
                {
                    minDist = distance;
                    shootingTarget = target;
                }
            }
            if (shootingTarget != null) return shootingTarget;

            #endregion
            
            #region Берсерк

            var berserkTarget = GetBerserkTarget();
            if (berserkTarget != null && berserkTarget.MyWizards.Any(x => x.Id == _self.Id))
            {
                _isBerserkTarget = true;
                return berserkTarget.Target;
            }

            #endregion

            #region Дохлый волшебник

            var wizards = _world.Wizards.Where(x => x.Faction != _self.Faction);

            //LivingUnit possibleShootingTarget = null;

            var minHp = double.MaxValue;
            //var possibleMinHp = double.MaxValue;

            foreach (var target in wizards)
            {
                if (target.Faction == _self.Faction) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                var life = target.Life;
                var distanceToRunForWeak = _self.CastRange * COEFF_TO_RUN_FOR_WEAK;

                if (IsWeakWizard(target) && _self.GetDistanceTo(target) <= distanceToRunForWeak)
                {
                    if (life < minHp)
                    {
                        minHp = life;
                        shootingTarget = target;
                    }
                }
            }

            if (shootingTarget != null) return shootingTarget;
            #endregion

            //#region Спокойный нейтрал

            //if (!IsCloseToWin() || GetLineAliveAnemyTowers(_line).Count > 0)
            //{
            //    var neutrals = _world.Minions.Where(x => x.Faction == Faction.Neutral);
            //    minHp = double.MaxValue;
            //    foreach (var target in neutrals)
            //    {
            //        if (!IsCalmNeutralMinion(target) || !ShouldAttackNeutralMinion(target)) continue;
            //        if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
            //        if (IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

            //        var life = target.Life;
            //        if (life < minHp)
            //        {
            //            minHp = life;
            //            shootingTarget = target;
            //        }
            //    }
            //}

            //if (shootingTarget != null) return shootingTarget;
            //#endregion

            #region Миньон
            var minions = _world.Minions;
            minHp = double.MaxValue;
            
            foreach (var target in minions)
            {
                if (target.Faction == _self.Faction) continue;
                if (target.Faction == Faction.Neutral && (IsCalmNeutralMinion(target) || !ShouldAttackNeutralMinion(target))) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                //double distance = _self.GetDistanceTo(target);

                var life = target.Life;
                if (life < minHp)
                {
                    minHp = life;
                    shootingTarget = target;
                }
            }

            if (shootingTarget != null) return shootingTarget;

            #endregion

            #region Обычный волшебник

            //LivingUnit possibleShootingTarget = null;

            minHp = double.MaxValue;
            //var possibleMinHp = double.MaxValue;

            foreach (var target in wizards)
            {
                if (target.Faction == _self.Faction) continue;
                //if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                if (_self.GetDistanceTo(target) > _self.CastRange * 1.5) continue;
                if (!IsOkToRunForWizard(_self, target)) continue;


                //var canShootWizard = CanShootWizard(_self, target, true, true, true);

                var life = target.Life;

                //if (canShootWizard)
                //{
                if (life < minHp)
                {
                    minHp = life;
                    shootingTarget = target;
                }
                //}
            }

            if (shootingTarget != null) return shootingTarget;
            #endregion

            #region Здание

            minDist = double.MaxValue;
            foreach (var target in _world.Buildings)
            {
                if (target.Faction == _self.Faction) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;
                if (target.Type != BuildingType.FactionBase && !IsStrongOnLine(target, _line)) continue;

                double distance = _self.GetDistanceTo(target);

                if (distance < minDist)
                {
                    minDist = distance;
                    shootingTarget = target;
                }
            }
            if (shootingTarget != null) return shootingTarget;

            #endregion




            return null;
        }

        private LivingUnit GetDefensiveLineShootingTarget()
        {
            LivingUnit shootingTarget = null;

            #region Дохлый волшебник

            var wizards = _world.Wizards.Where(x => x.Faction != _self.Faction);

            //LivingUnit possibleShootingTarget = null;

            var minHp = double.MaxValue;
            //var possibleMinHp = double.MaxValue;

            foreach (var target in wizards)
            {
                if (target.Faction == _self.Faction) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                var life = target.Life;
                var distanceToRunForWeak = _self.CastRange * COEFF_TO_RUN_FOR_WEAK;

                if (IsWeakWizard(target) && _self.GetDistanceTo(target) <= distanceToRunForWeak)
                {
                    if (life < minHp)
                    {
                        minHp = life;
                        shootingTarget = target;
                    }
                }
            }

            if (shootingTarget != null) return shootingTarget;
            #endregion

            #region Спокойный нейтрал

            if (!IsCloseToWin() || GetLineAliveAnemyTowers(_line).Count > 0)
            {
                var neutrals = _world.Minions.Where(x => x.Faction == Faction.Neutral);
                minHp = double.MaxValue;
                foreach (var target in neutrals)
                {
                    if (!IsCalmNeutralMinion(target) || !ShouldAttackNeutralMinion(target)) continue;
                    if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                    if (IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                    var life = target.Life;
                    if (life < minHp)
                    {
                        minHp = life;
                        shootingTarget = target;
                    }
                }
            }

            if (shootingTarget != null) return shootingTarget;
            #endregion

            #region Дохлая башня

            var minDist = double.MaxValue;
            foreach (var target in _world.Buildings)
            {
                if (target.Faction == _self.Faction) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;
                if (!IsStrongOnLine(target, _line)) continue;

                double distance = _self.GetDistanceTo(target);

                if (distance < minDist && target.Life <= target.MaxLife * 0.5)
                {
                    minDist = distance;
                    shootingTarget = target;
                }
            }
            if (shootingTarget != null) return shootingTarget;

            #endregion
            
            #region Опасный миньон
            var minions = _world.Minions;
            minHp = double.MaxValue;
            foreach (var target in minions.Where(x => x.Type == MinionType.FetishBlowdart && x.GetDistanceTo(_self) < GetAttackRange(x)))
            {
                if (target.Faction == _self.Faction) continue;
                if (target.Faction == Faction.Neutral && !ShouldAttackNeutralMinion(target)) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                //double distance = _self.GetDistanceTo(target);

                var life = target.Life;
                if (life < minHp)
                {
                    minHp = life;
                    shootingTarget = target;
                }
            }

            if (shootingTarget != null) return shootingTarget;

            #endregion

            #region Миньон
            minHp = double.MaxValue;
            foreach (var target in minions)
            {
                if (target.Faction == _self.Faction) continue;
                if (target.Faction == Faction.Neutral && !ShouldAttackNeutralMinion(target)) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                //double distance = _self.GetDistanceTo(target);

                var life = target.Life;
                if (life < minHp)
                {
                    minHp = life;
                    shootingTarget = target;
                }
            }

            if (shootingTarget != null) return shootingTarget;

            #endregion

            #region Обычная башня
            minDist = double.MaxValue;
            foreach (var target in _world.Buildings)
            {
                if (target.Faction == _self.Faction) continue;
                if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;
                if (!IsStrongOnLine(target, _line)) continue;

                double distance = _self.GetDistanceTo(target);

                if (distance < minDist && target.Life <= target.MaxLife)
                {
                    minDist = distance;
                    shootingTarget = target;
                }
            }
            if (shootingTarget != null) return shootingTarget;

            #endregion
            
            #region Обычный волшебник

            //LivingUnit possibleShootingTarget = null;

            minHp = double.MaxValue;
            //var possibleMinHp = double.MaxValue;

            foreach (var target in wizards)
            {
                if (target.Faction == _self.Faction) continue;
                //if (!IsOkDistanceToShoot(_self, target, 0d)) continue;
                if (!_isOneOneOne && IsBlockingTree(_self, target, _game.MagicMissileRadius)) continue;

                if (_self.GetDistanceTo(target) > _self.CastRange * 1.5) continue;
                if (!IsOkToRunForWizard(_self, target)) continue;


                //var canShootWizard = CanShootWizard(_self, target, true, true, true);

                var life = target.Life;

                //if (canShootWizard)
                //{
                if (life < minHp)
                {
                    minHp = life;
                    shootingTarget = target;
                }
                //}
            }

            if (shootingTarget != null) return shootingTarget;
            #endregion

         
            return null;
        }



        private double GetShootingCooldown(Wizard wizard)
        {
            var missleCooldown = wizard.RemainingCooldownTicksByAction[(int) ActionType.MagicMissile];
            var fireballCooldown = wizard.Skills.Any(x => x == SkillType.Fireball)
                ? wizard.RemainingCooldownTicksByAction[(int) ActionType.Fireball]
                : double.MaxValue;
            var frostboltCooldown = wizard.Skills.Any(x => x == SkillType.FrostBolt)
                ? wizard.RemainingCooldownTicksByAction[(int) ActionType.FrostBolt]
                : double.MaxValue;

            var cooldown = Math.Min(missleCooldown, fireballCooldown);
            cooldown = Math.Min(cooldown, frostboltCooldown);
            cooldown = Math.Max(cooldown, wizard.RemainingActionCooldownTicks);
            return cooldown;
        }

        private bool IsOkToRunForWizard(Wizard source, Wizard target)
        {
            var targetCooldown = GetShootingCooldown(target);

            var anemyWizards = _world.Wizards.Where(x => x.Faction != _self.Faction && GetShootingCooldown(target) < GetShootingCooldown(source));

            //if (isAgressive && sourceCooldown >= targetCooldown || !isAgressive && sourceCooldown > targetCooldown)
            //{
            //    return false;
            //}

            var newSourceX = source.X +
                             GetWizardMaxForwardSpeed(source)*Math.Cos(source.Angle + source.GetAngleTo(target)) * targetCooldown;
            var newSourceY = source.Y +
                             GetWizardMaxForwardSpeed(source) * Math.Sin(source.Angle + source.GetAngleTo(target)) * targetCooldown;

            var newSource= new Wizard(
                       source.Id,
                       newSourceX,
                       newSourceY,
                       source.SpeedX,
                       source.SpeedY,
                       source.Angle,
                       source.Faction,
                       source.Radius,
                       source.Life,
                       source.MaxLife,
                       source.Statuses,
                       source.OwnerPlayerId,
                       source.IsMe,
                       source.Mana,
                       source.MaxMana,
                       source.VisionRange,
                       source.CastRange,
                       source.Xp,
                       source.Level,
                       source.Skills,
                       source.RemainingActionCooldownTicks,
                       source.RemainingCooldownTicksByAction,
                       source.IsMaster,
                       source.Messages);

            var hasDangerousWizards = anemyWizards.Any(x => CanShootWizard(x, newSource, false, true, true));

            if (hasDangerousWizards)
            {

                return CanShootWizardWithMissleNoCooldown(source, source.X, source.Y, target, 0, true, true, true);
              
            }
            else
            {
                var newTargetX = target.X +
                               GetWizardMaxBackSpeed(target) * Math.Cos(target.Angle + target.GetAngleTo(source) - Math.PI) *
                               targetCooldown;
                var newTargetY = target.Y +
                                 GetWizardMaxBackSpeed(target) * Math.Sin(target.Angle + target.GetAngleTo(source) - Math.PI) *
                                 targetCooldown;

                var newTarget = new Wizard(
                    target.Id,
                    newTargetX,
                    newTargetY,
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

                return CanShootWizardWithMissleNoCooldown(source, newSourceX, newSourceY, newTarget, 0, true, true, true);
            }
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

        private double GetAttackRange(LivingUnit unit)
        {
            var building = unit as Building;
            if (building != null) return building.AttackRange;

            var wizard = unit as Wizard;
            if (wizard != null) return wizard.CastRange + _self.Radius - _game.MagicMissileRadius;

            var minion = unit as Minion;
            if (minion != null)
            {
                if (minion.Type == MinionType.FetishBlowdart)
                {
                    return _game.FetishBlowdartAttackRange + _self.Radius - _game.DartRadius;
                }
                else
                {
                    return _game.OrcWoodcutterAttackRange + _self.Radius;
                }
            }

            return 0;
        }
    
        private bool IsBlockingTree(LivingUnit source, LivingUnit target, double bulletRadius)
        {
            foreach (var tree in _trees)
            {
                var isCross = Square.Intersect(source.X, source.Y, target.X, target.Y, tree.X, tree.Y, bulletRadius, tree.Radius);
                if (isCross) return true;
            }
            return false;
        }
       
        private bool IsTotallyOnLine(LivingUnit livingUnit, LaneType laneType)
        {
            var isNearToBase = livingUnit.X <= 2 * ROW_WIDTH && livingUnit.Y >= _world.Height - 2 * ROW_WIDTH;

            if (isNearToBase) return false;

            var isOnTop = livingUnit.Y <= ROW_WIDTH || livingUnit.X <= ROW_WIDTH;
                     
            if (laneType == LaneType.Top)
            {
                return isOnTop;
            }

            var isOnBottom = livingUnit.Y >= _world.Height - ROW_WIDTH || livingUnit.X >= _world.Width - ROW_WIDTH;
                            
            if (laneType == LaneType.Bottom)
            {
                return isOnBottom;
            }

            var isOnMainDiagonal = livingUnit.Y >= _world.Width - ROW_WIDTH - livingUnit.X &&
                                   livingUnit.Y <= _world.Width + ROW_WIDTH - livingUnit.X;

            //Mid
            return isOnMainDiagonal;
        }

        private LaneType? GetLineType(LivingUnit unit)
        {
            if (IsTotallyOnLine(unit, LaneType.Top)) return LaneType.Top;
            if (IsTotallyOnLine(unit, LaneType.Middle)) return LaneType.Middle;
            if (IsTotallyOnLine(unit, LaneType.Bottom)) return LaneType.Bottom;
            return null;

        }

        private bool IsStrongOnLine(LivingUnit livingUnit, LaneType laneType)
        {
            var isNearToBase = livingUnit.X <= 2 * ROW_WIDTH && livingUnit.Y >= _world.Height - 2 * ROW_WIDTH;

            if (isNearToBase) return false;

            var isOnTop = livingUnit.Y <= ROW_WIDTH || livingUnit.X <= ROW_WIDTH ||
                        livingUnit.Y <= _world.Width / 2 - livingUnit.X;

            if (laneType == LaneType.Top)
            {
                return isOnTop;
            }

            var isOnBottom = livingUnit.Y >= _world.Height - ROW_WIDTH || livingUnit.X >= _world.Width - ROW_WIDTH ||
                             livingUnit.Y >= 3 * _world.Width / 2 - livingUnit.X;
            if (laneType == LaneType.Bottom)
            {
                return isOnBottom;
            }

            var isOnMainDiagonal = livingUnit.Y >= _world.Width - ROW_WIDTH - livingUnit.X &&
                                   livingUnit.Y <= _world.Width + ROW_WIDTH - livingUnit.X;

            //Mid
            return isOnMainDiagonal;
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
                if (w.Skills.Contains(SkillType.MovementBonusFactorAura2))
                {
                    maxAura = SkillType.MovementBonusFactorAura2;
                    break;
                }
                if (w.Skills.Contains(SkillType.MovementBonusFactorAura1))
                {
                    maxAura = SkillType.MovementBonusFactorAura1;
                }
            }

            if (maxAura == SkillType.MovementBonusFactorAura2)
            {
                resultSpeed += defaultSpeed * 0.1;
            }
            else if (maxAura == SkillType.MovementBonusFactorAura1)
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

        private bool IsOkToDestroyBase(LivingUnit target)
        {
            if (target == null) return false;
            var building = target as Building;
            if (building == null || building.Type != BuildingType.FactionBase) return false;

            var isWeakBuilding = target.Life <= target.MaxLife*0.5;
            var isOkHp = _self.Life > _self.MaxLife*0.25;

            var anemyMinions = _world.Minions.Where(x => x.Faction != _self.Faction);
            var isFarMinios = true;
            foreach (var minion in anemyMinions)
            {
                if (minion.Faction == Faction.Neutral && IsCalmNeutralMinion(minion) || minion.Type == MinionType.FetishBlowdart) continue;
                if (minion.GetDistanceTo(_self) - _self.Radius <= _game.OrcWoodcutterAttackRange)
                {
                    isFarMinios = false;
                    break;
                }
            }

            var anemyWizards = _world.Wizards.Where(x => x.Faction != _self.Faction && x.Id != target.Id);
            var isFarWizards = anemyWizards.All(x => !CanShootWizard(x, _self, true, false, false));

            return isWeakBuilding && isOkHp && isFarMinios && isFarWizards;
        }

        private bool IsCloseToWin()
        {
            return _self.GetDistanceTo(_anemyBaseX, _anemyBaseY) < CLOSE_TO_WIN_DISTANCE;
        }

        private IList<Building> GetLineAliveAnemyTowers(LaneType lane)
        {
            var towers = new List<Building>();
            for (int i = 0; i < _anemyBuildings.Count; ++i)
            {
                if (!_IsAnemyBuildingAlive[i]) continue;
                if (_anemyBuildings[i].Type == BuildingType.FactionBase) continue;
                if (!IsStrongOnLine(_anemyBuildings[i], lane)) continue;

                towers.Add(_anemyBuildings[i]);
            }
            return towers;
        }

        private bool CanDestroyTower()
        {
            var lineCoeff = GetLineCoeff(_myWizards[_line].Count, _anemyWizards[_line].Count);
            if (lineCoeff < 1) return false;

            var towers = GetLineAliveAnemyTowers(_line);
            var orderedTowers = towers.OrderBy(x => x.GetDistanceTo(_self));
            var tower = orderedTowers.FirstOrDefault();
            if (tower == null) return false;

             var units = new List<LivingUnit>();
            units.AddRange(_world.Wizards.Where(x => x.Faction == _self.Faction));
            units.AddRange(_world.Minions.Where(x => x.Faction == _self.Faction));
            var nearUnits = units.Where(x => x.GetDistanceTo(tower) - tower.Radius <= GetAttackRange(x));

            return (tower.GetDistanceTo(_self) <= CLOSE_TO_TOWER_DISTANCE) &&
                tower.Life <= tower.MaxLife*TOWER_HP_FACTOR && nearUnits.Count() >= 3;
        }

    
        private BerserkTargetResult GetBerserkTarget()
        {
            if (!_isOneOneOne) return null;
            //if (_self.IsMaster) return null;

            //if (_seenAnemyWizards.Count < 5) return null;

            var isFarBuildings = true;
            var friends = new List<LivingUnit>();
            friends.AddRange(_world.Wizards.Where(x => x.Faction == _self.Faction));
            friends.AddRange(_world.Minions.Where(x => x.Faction == _self.Faction));
            friends.AddRange(_world.Buildings.Where(x => x.Faction == _self.Faction));


            for (int i = 0; i < _anemyBuildings.Count; ++i)
            {
                if (!_IsAnemyBuildingAlive[i]) continue;
                //if (_anemyBuildings[i].GetDistanceTo(_self) - _self.Radius < _anemyBuildings[i].AttackRange)
                //{
                //    isFarBuildings = false;
                //    break;
                //}


                if (!CanGoToBuilding(_anemyBuildings[i], friends))
                {
                    isFarBuildings = false;
                    break;
                }
            }

            var anemyMinions = _world.Minions.Where(x => x.Faction != _self.Faction);
            var isFarMinios = true;
            foreach (var minion in anemyMinions)
            {
                if (minion.Faction == Faction.Neutral && IsCalmNeutralMinion(minion) || minion.Type == MinionType.FetishBlowdart) continue;
                if (minion.GetDistanceTo(_self) - _self.Radius <= _game.OrcWoodcutterAttackRange)
                {
                    isFarMinios = false;
                    break;
                }
            }

            if (!isFarBuildings || !isFarMinios) return null;

            var okAnemyWizards = new List<BerserkTargetResult>();

            foreach (var wizard in _world.Wizards.Where(x => x.Faction != _self.Faction))
            {
                LaneType? laneType = null;
                foreach (var lt in _anemyWizards.Keys)
                {
                    if (_anemyWizards[lt].Contains(wizard.Id))
                    {
                        laneType = lt;
                        break;
                    }
                }
                if (laneType == null) continue;

                var lineType = GetMyLineType(laneType.Value);
                if (lineType != LineType.Agressive) continue;

                var myWizards = _world.Wizards.Where(x => x.Faction == _self.Faction);

                var nearMyWizards = myWizards.Where(x => x.GetDistanceTo(wizard) <= x.CastRange * BERSERK_COEFF);

                var nearAnemyWizards = _world.Wizards.Where(
                    x => x.Faction != _self.Faction && nearMyWizards.Any(y => x.GetDistanceTo(y) <= x.CastRange * BERSERK_COEFF));

                if (nearMyWizards.Count() > nearAnemyWizards.Count()) okAnemyWizards.Add(new BerserkTargetResult()
                {
                    Target = wizard,
                    MyWizards = nearMyWizards
                });
            }

            if (!okAnemyWizards.Any()) return null;
            var sortedOkAnemyWizards = okAnemyWizards.OrderBy(x => x.Target.Life);
            return sortedOkAnemyWizards.First();
        }

        private bool IsOkToRunForWeakWizard(LivingUnit target)
        {
            if (target == null) return false;

            var isWeakWizard = IsWeakWizard(target);
            var isOkHp = _self.Life > target.Life;
            var isFarBuildings = true;
            for (int i = 0; i < _anemyBuildings.Count; ++i)
            {
                if (!_IsAnemyBuildingAlive[i] || _self.Life > _anemyBuildings[i].Damage) continue;
                if (_anemyBuildings[i].GetDistanceTo(_self) - _self.Radius < _anemyBuildings[i].AttackRange)
                {
                    isFarBuildings = false;
                    break;
                }
            }

            var anemyWizards = _world.Wizards.Where(x => x.Faction != _self.Faction && x.Id != target.Id);
            var isFarWizards = anemyWizards.All(x => !CanShootWizard(x, _self, true, false, false));

            var anemyMinions = _world.Minions.Where(x => x.Faction != _self.Faction);
            var isFarMinios = true;
            foreach (var minion in anemyMinions)
            {
                if (minion.Faction == Faction.Neutral && IsCalmNeutralMinion(minion) || minion.Type == MinionType.FetishBlowdart) continue;
                if (minion.GetDistanceTo(_self) - _self.Radius <= _game.OrcWoodcutterAttackRange)
                {
                    isFarMinios = false;
                    break;
                }
            }

            return isWeakWizard && isOkHp && isFarBuildings && isFarWizards && isFarMinios;
        }

        private bool CanGoToStaffRange(LivingUnit shootingTarget, double selfNextTickX, double selfNextTickY, out IList<Wizard> goBackWizard)
        {
            goBackWizard = new List<Wizard>();

            if (IsOkToRunForWeakWizard(shootingTarget)) return true;
            if (_isBerserkTarget) return true;
            if (IsOkToDestroyBase(shootingTarget)) return true;

            var friends = new List<LivingUnit>();
            friends.AddRange(_world.Wizards.Where(x => x.Faction == _self.Faction));
            friends.AddRange(_world.Minions.Where(x => x.Faction == _self.Faction));
            friends.AddRange(_world.Buildings.Where(x => x.Faction == _self.Faction));

            if (friends.Count < 2) return false;

            var anemies = new List<LivingUnit>();
            var anemyWizard =
                _world.Wizards.Where(x => x.Faction != _self.Faction && x.GetDistanceTo(_self) <= _self.CastRange*2);
            anemies.AddRange(anemyWizard);
            var anemyMinions =
                _world.Minions.Where(
                    x =>
                        x.Faction != _self.Faction && (x.Faction != Faction.Neutral || !IsCalmNeutralMinion(x)) &&
                        x.GetDistanceTo(_self) <= _self.CastRange*1.5);
            anemies.AddRange(anemyMinions);


            for (int i = 0; i < _anemyBuildings.Count; ++i)
            {
                if (!_IsAnemyBuildingAlive[i]) continue;
                if (!IsStrongOnLine(_anemyBuildings[i], _line)) continue;

                if (_anemyBuildings[i].GetDistanceTo(_self) <
                    _anemyBuildings[i].AttackRange + 2.5 * _self.Radius)
                {
                    var realBuilding =
                        _world.Buildings.FirstOrDefault(
                            b =>
                                Math.Abs(b.X - _anemyBuildings[i].X) < TOLERANCE &&
                                Math.Abs(b.Y - _anemyBuildings[i].Y) < TOLERANCE);

                    anemies.Add(realBuilding ?? _anemyBuildings[i]);
                }
            }

            
            var needRunBackAnemies = new List<LivingUnit>();
            foreach (var anemy in anemies)
            {
                //var needRunBack = NeedRunBack(anemy, _self, friends, ref runBackTime);
                var needRunBack = NeedRunBack(anemy, _self, friends, selfNextTickX, selfNextTickY);
                if (needRunBack)
                {
                    needRunBackAnemies.Add(anemy);
                    if (anemy is Wizard) goBackWizard.Add(anemy as Wizard);
                }
            }

            if (!needRunBackAnemies.Any()) return true;//0
            if (needRunBackAnemies.Count > 1) return false;//>=2
            //одинокий миьон
            if (needRunBackAnemies[0] is Minion)
            {
                var eps = _self.Radius/2;
                return !anemyWizard.Any() &&
                       anemyMinions.All(x => x.Id == needRunBackAnemies[0].Id || x.GetDistanceTo(_self) > _self.CastRange)
                       && (_self.Life > _self.MaxLife * 0.75 || _self.GetDistanceTo(needRunBackAnemies[0]) > GetAttackRange(needRunBackAnemies[0]) + eps);
            }
           
            return false;
            

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

            var resultLine = _isLineSet ? _line : LaneType.Middle;

            //var berserkTargetResult = GetBerserkTarget();
            //var myWizardsIds = new List<long>();
            //if (berserkTargetResult != null)
            //{
            //    myWizardsIds.AddRange(berserkTargetResult.MyWizards.Select(x => x.Id));
            //}
            //for (int i = 0; i < myWizardsIds.Count; ++i)
            //{
            //    if (myWizardsIds[i] > 5) myWizardsIds[i] -= 5;
            //}

            _move.Messages = new Message[]
            {
                new Message(resultLine, null, new byte[0]),
                new Message(resultLine, null, new byte[0]),
                new Message(resultLine, null, new byte[0]),
                new Message(resultLine, null, new byte[0])
            };

            //_move.Messages = new Message[]
            //{
            //    new Message(
            //        resultLine,
            //        null,
            //        berserkTargetResult != null && myWizardsIds.Contains(2)
            //            ? new byte[] {(byte) berserkTargetResult.Target.Id}
            //            : new byte[0]),
            //    new Message(
            //        resultLine,
            //        null,
            //        berserkTargetResult != null && myWizardsIds.Contains(3)
            //            ? new byte[] {(byte) berserkTargetResult.Target.Id}
            //            : new byte[0]),
            //    new Message(
            //        resultLine,
            //        null,
            //        berserkTargetResult != null && myWizardsIds.Contains(4)
            //            ? new byte[] {(byte) berserkTargetResult.Target.Id}
            //            : new byte[0]),
            //    new Message(
            //        resultLine,
            //        null,
            //        berserkTargetResult != null && myWizardsIds.Contains(5)
            //            ? new byte[] {(byte) berserkTargetResult.Target.Id}
            //            : new byte[0]),
            //};
            
        }
        

        private LaneType GetNearToBaseLine()
        {
            var laneTypes = new List<LaneType>()
            {
                LaneType.Middle,
                LaneType.Top,
                LaneType.Bottom
            };

            var dangerousLines = new List<LaneType>();
            foreach (var laneType in laneTypes)
            {
                if (_world.Buildings.Any(x => x.Faction == _self.Faction 
                    && x.Type != BuildingType.FactionBase && IsStrongOnLine(x, laneType)))
                {
                    continue;
                }
                dangerousLines.Add(laneType);
            }

            if (!dangerousLines.Any()) return GetAgressiveLineToGo(false).Value;

            var resultLane = dangerousLines.First();
            var minDist = double.MaxValue;
            foreach (var lane in dangerousLines)
            {
                var nearestAnemy = GetNearestMyBaseAnemy(lane);
                if (nearestAnemy != null)
                {
                    var dist = nearestAnemy.GetDistanceTo(_world.Buildings.First(
                        x => x.Faction == _self.Faction && x.Type == BuildingType.FactionBase));
                    if (dist < minDist )
                    {
                        minDist = dist;
                        resultLane = lane;
                    }
                }
            }

            return resultLane;
        }
        private double GetLineCoeff(int myWizardsCount, int anemyWizardsCount)
        {
            var correctMyWizardsCount = myWizardsCount;
            var correctAnemyWizardsCount = anemyWizardsCount;

            if (myWizardsCount == 0 || anemyWizardsCount == 0)
            {
                correctMyWizardsCount++;
                correctAnemyWizardsCount++;
            }
            return correctMyWizardsCount * 1d / correctAnemyWizardsCount;
        }
       
        private LaneType GetOptimalLine(LaneType excludingLaneType)
        {
            var exceludingLineCoeff = GetLineCoeff(_myWizards[excludingLaneType].Count,
                _anemyWizards[excludingLaneType].Count);

            var isAgressiveExcludingLine = exceludingLineCoeff > 1;

            var optimalLine = _line;
            var myWiardsOnOptimalLine = _myWizards[optimalLine].Count;
            var optimalCoeff = GetLineCoeff(_myWizards[optimalLine].Count, _anemyWizards[optimalLine].Count);

            foreach (var lane in _myWizards.Keys.Where(x => x != excludingLaneType))
            {
//                if (_myWizards[lane].Count == 0 && _anemyWizards[lane].Count == 1)
//                {
//                    return lane; //сдерживаем одинокого
//                }

                var coeff = GetLineCoeff(_myWizards[lane].Count, _anemyWizards[lane].Count);

                if (isAgressiveExcludingLine)
                {
                    if (coeff < optimalCoeff || coeff <= optimalCoeff && _myWizards[lane].Count < myWiardsOnOptimalLine)
                    {
                        optimalLine = lane;
                        myWiardsOnOptimalLine = _myWizards[lane].Count;
                    }
                }
                else
                {
                    if (coeff > optimalCoeff || coeff >= optimalCoeff && _myWizards[lane].Count < myWiardsOnOptimalLine)
                    {
                        optimalLine = lane;
                        myWiardsOnOptimalLine = _myWizards[lane].Count;
                    }
                }

            }

            return optimalLine;
        }
        
        private LaneType? GetAgressiveLineToGo(bool needTwoWizards)
        {
            var laneTypes = new List<LaneType>()
            {
                LaneType.Middle,
                LaneType.Top,
                LaneType.Bottom
            };
            var laneWizards = new Dictionary<LaneType, int>()
            {
                {LaneType.Middle, 0},
                {LaneType.Top, 0},
                {LaneType.Bottom, 0},
            };

            foreach (var wizard in _world.Wizards.Where(x => x.Faction == _self.Faction && !x.IsMe))
            {
                foreach (var laneType in laneTypes)
                {
                    if (IsStrongOnLine(wizard, laneType))
                    {
                        laneWizards[laneType]++;
                    }
                }
            }

            if (needTwoWizards)
            {
                return laneWizards.Keys.Any(x => laneWizards[x] >= 2)
                    ? laneWizards.Keys.FirstOrDefault(x => laneWizards[x] >= 2)
                    : (LaneType?) null;
            }

           
            var resultLane = _line;
            var maxLaneWizards = laneWizards[resultLane];
            foreach (var laneType in laneTypes)
            {
                if (laneWizards[laneType] > maxLaneWizards)
                {
                    maxLaneWizards = laneWizards[laneType];
                    resultLane = laneType;
                }
            }

            return resultLane;
        }
        
        private SkillType GetSkillTypeToLearn()
        {
            var skillsOrder = _isOneOneOne ? _commonSkillsOrder[(int) ((_self.Id - 1)%5)] : _commonSkillsOrder[0];
            var newSkill = skillsOrder.FirstOrDefault(x => !_self.Skills.Contains(x));
            return newSkill;
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
            public GoToResult GoToResult { get; set; }
        }

        private class GoToResult
        {
            public double X { get; set;}
            public double Y { get; set;}
            public Tree WoodCuTree { get; set;}
        }

        private enum LineType
        {
            Agressive,
            Defensive,
            Neutral
        }

        private class BerserkTargetResult
        {
            public Wizard Target { get; set; }
            public IEnumerable<Wizard> MyWizards { get; set; } 
        }

    }
}