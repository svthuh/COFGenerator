using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace CourseOfFire
{
    static class Disciplines
    {
        public const string Rifle = "Precision rifle";
        public const string Rimfire = "NRL22 / rimfire";
        public const string Pistol = "Pistol / carbine";
        public static readonly string[] All = { Rifle, Rimfire, Pistol };

        public static string DefaultStart(string discipline)
        {
            return discipline == Pistol
                ? "Standing in Box A, wrists above shoulders, gun loaded and holstered"
                : "Standing at position 1, rifle staged with bolt open";
        }
    }

    [DataContract]
    class StageTarget
    {
        [DataMember(Name = "type")] public string TypeId;
        [DataMember(Name = "size")] public string Size;
        [DataMember(Name = "distance")] public int Distance;
        [DataMember(Name = "unit")] public string Unit;
        [DataMember(Name = "hits")] public int Hits;
        [DataMember(Name = "notes")] public string Notes;

        public static StageTarget Of(string typeId, int distance, string size = null, string notes = "")
        {
            var type = Catalog.Target(typeId);
            return new StageTarget
            {
                TypeId = type.Id,
                Size = size ?? type.Sizes[0],
                Distance = distance,
                Unit = "yd",
                Hits = 1,
                Notes = notes
            };
        }

        public string DistanceText
        {
            get { return Distance > 0 ? Distance + " " + Unit : ""; }
        }

        public int DistanceInYards
        {
            get { return Unit == "m" ? (int)Math.Round(Distance * 1.0936) : Distance; }
        }

        public StageTarget Clone()
        {
            return (StageTarget)MemberwiseClone();
        }

        public void Normalize()
        {
            TypeId = Catalog.Target(TypeId).Id;
            if (Size == null) Size = "";
            if (Unit != "m") Unit = "yd";
            if (Hits < 1) Hits = 1;
            if (Notes == null) Notes = "";
        }
    }

    [DataContract]
    class StageProp
    {
        [DataMember(Name = "type")] public string TypeId;
        [DataMember(Name = "name")] public string CustomName;

        public static StageProp Of(string typeId)
        {
            return new StageProp { TypeId = typeId, CustomName = "" };
        }

        public string DisplayName
        {
            get { return string.IsNullOrEmpty(CustomName) ? Catalog.Prop(TypeId).Name : CustomName; }
        }

        public StageProp Clone()
        {
            return (StageProp)MemberwiseClone();
        }

        public void Normalize()
        {
            TypeId = Catalog.Prop(TypeId).Id;
            if (CustomName == null) CustomName = "";
        }
    }

    [DataContract]
    class Stage
    {
        [DataMember(Name = "id")] public string Id;
        [DataMember(Name = "name")] public string Name;
        [DataMember(Name = "discipline")] public string Discipline;
        [DataMember(Name = "targets")] public List<StageTarget> Targets;
        [DataMember(Name = "props")] public List<StageProp> Props;
        [DataMember(Name = "positions")] public List<string> Positions;
        [DataMember(Name = "parTime")] public int ParTime;
        [DataMember(Name = "start")] public string Start;
        [DataMember(Name = "procedure")] public string Procedure;
        [DataMember(Name = "notes")] public string Notes;
        [DataMember(Name = "created")] public string Created;
        [DataMember(Name = "modified")] public string Modified;

        public Stage()
        {
            Id = NewId();
            Name = "New stage";
            Discipline = Disciplines.Rifle;
            Targets = new List<StageTarget>();
            Props = new List<StageProp>();
            Positions = new List<string>();
            ParTime = 120;
            Start = "";
            Procedure = "";
            Notes = "";
            Created = Modified = DateTime.Now.ToString("o");
        }

        public static string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }

        public int RoundCount
        {
            get { return Targets.Sum(t => Math.Max(1, t.Hits)); }
        }

        public DateTime ModifiedTime
        {
            get
            {
                DateTime d;
                return DateTime.TryParse(Modified, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out d)
                    ? d : DateTime.MinValue;
            }
        }

        public void Touch()
        {
            Modified = DateTime.Now.ToString("o");
        }

        public Stage Copy()
        {
            var copy = (Stage)MemberwiseClone();
            copy.Id = NewId();
            copy.Targets = Targets.Select(t => t.Clone()).ToList();
            copy.Props = Props.Select(p => p.Clone()).ToList();
            copy.Positions = new List<string>(Positions);
            copy.Created = copy.Modified = DateTime.Now.ToString("o");
            return copy;
        }

        // Deserialized objects skip constructors, so fill in anything missing.
        public void Normalize()
        {
            if (string.IsNullOrEmpty(Id)) Id = NewId();
            if (string.IsNullOrWhiteSpace(Name)) Name = "Untitled stage";
            if (!Disciplines.All.Contains(Discipline)) Discipline = Disciplines.Rifle;
            if (Targets == null) Targets = new List<StageTarget>();
            if (Props == null) Props = new List<StageProp>();
            if (Positions == null) Positions = new List<string>();
            Targets.RemoveAll(t => t == null);
            Props.RemoveAll(p => p == null);
            Positions.RemoveAll(p => p == null);
            foreach (var t in Targets) t.Normalize();
            foreach (var p in Props) p.Normalize();
            if (ParTime < 1) ParTime = 120;
            if (Start == null) Start = "";
            if (Procedure == null) Procedure = "";
            if (Notes == null) Notes = "";
            if (string.IsNullOrEmpty(Created)) Created = DateTime.Now.ToString("o");
            if (string.IsNullOrEmpty(Modified)) Modified = Created;
        }
    }

    // The targets, props and positions available on the user's range, plus the
    // rules the generator follows for one discipline.
    [DataContract]
    class RangeSetup
    {
        [DataMember(Name = "discipline")] public string Discipline;
        [DataMember(Name = "targets")] public List<StageTarget> Targets;
        [DataMember(Name = "props")] public List<string> PropIds;
        [DataMember(Name = "positions")] public List<string> Positions;
        [DataMember(Name = "minTargets")] public int MinTargets;
        [DataMember(Name = "maxTargets")] public int MaxTargets;
        [DataMember(Name = "minHits")] public int MinHits;
        [DataMember(Name = "maxHits")] public int MaxHits;
        [DataMember(Name = "minTime")] public int MinTime;
        [DataMember(Name = "maxTime")] public int MaxTime;
        [DataMember(Name = "minProps")] public int MinProps;
        [DataMember(Name = "maxProps")] public int MaxProps;
        [DataMember(Name = "positionCount")] public int PositionCount;
        [DataMember(Name = "requireMovement")] public bool RequireMovement;
        [DataMember(Name = "supportSide")] public bool SupportSide;
        [DataMember(Name = "switchSecondary")] public bool SwitchSecondary;
        [DataMember(Name = "nearToFar")] public bool NearToFar;

        public void Normalize()
        {
            if (Targets == null) Targets = new List<StageTarget>();
            if (PropIds == null) PropIds = new List<string>();
            if (Positions == null) Positions = new List<string>();
            Targets.RemoveAll(t => t == null);
            foreach (var t in Targets) t.Normalize();
            PropIds.RemoveAll(id => !Catalog.HasProp(id));
            MinTargets = Math.Max(1, MinTargets);
            MaxTargets = Math.Max(MinTargets, MaxTargets);
            MinHits = Math.Max(1, MinHits);
            MaxHits = Math.Max(MinHits, MaxHits);
            MinTime = Math.Max(1, MinTime);
            MaxTime = Math.Max(MinTime, MaxTime);
            MinProps = Math.Max(0, MinProps);
            MaxProps = Math.Max(MinProps, MaxProps);
            PositionCount = Math.Max(1, PositionCount);
        }
    }

    [DataContract]
    class Library
    {
        [DataMember(Name = "version")] public int Version;
        [DataMember(Name = "stages")] public List<Stage> Stages;
        [DataMember(Name = "ranges")] public List<RangeSetup> Ranges;

        public RangeSetup Range(string discipline)
        {
            var range = Ranges.FirstOrDefault(r => r.Discipline == discipline);
            if (range == null)
            {
                range = Defaults.Range(discipline);
                Ranges.Add(range);
            }
            return range;
        }

        public void ResetRange(string discipline)
        {
            Ranges.RemoveAll(r => r.Discipline == discipline);
            Ranges.Add(Defaults.Range(discipline));
        }

        public void Normalize()
        {
            Version = 1;
            if (Stages == null) Stages = new List<Stage>();
            if (Ranges == null) Ranges = new List<RangeSetup>();
            Stages.RemoveAll(s => s == null);
            Ranges.RemoveAll(r => r == null || !Disciplines.All.Contains(r.Discipline));
            foreach (var s in Stages) s.Normalize();
            foreach (var r in Ranges) r.Normalize();
        }
    }

    // A file of stages shared between copies of the app (.cof).
    [DataContract]
    class CourseFile
    {
        [DataMember(Name = "format")] public string Format;
        [DataMember(Name = "version")] public int Version;
        [DataMember(Name = "stages")] public List<Stage> Stages;
    }

    class TargetType
    {
        public string Id, Name, Shape, Category, Hint;
        public string[] Sizes;
    }

    class PropType
    {
        public string Id, Name, Hint;
    }

    static class Catalog
    {
        public static readonly string[] TargetCategories = { "Steel", "Paper", "Reactive" };

        public static readonly List<TargetType> Targets = new List<TargetType>
        {
            T("gong", "Round gong", "gong", "Steel", "Hanging round plate", "10 in", "2 in", "3 in", "4 in", "6 in", "8 in", "12 in", "18 in", "24 in"),
            T("square", "Square plate", "square", "Steel", "Hanging square plate", "8 in", "2 in", "3 in", "4 in", "6 in", "10 in", "12 in"),
            T("rect", "Rectangle plate", "rect", "Steel", "Tall hanging plate", "6 x 12 in", "4 x 8 in", "8 x 16 in", "12 x 18 in", "12 x 24 in"),
            T("ipsc_full", "Full IPSC steel", "ipsc", "Steel", "18 x 30 in silhouette", "18 x 30 in"),
            T("ipsc_23", "2/3 IPSC steel", "ipsc", "Steel", "12 x 20 in silhouette", "12 x 20 in"),
            T("ipsc_13", "1/3 IPSC steel", "ipsc", "Steel", "6 x 10 in silhouette", "6 x 10 in"),
            T("diamond", "Diamond plate", "diamond", "Steel", "Plate hung on a point", "8 in", "6 in", "10 in", "12 in"),
            T("triangle", "Triangle plate", "triangle", "Steel", "Point-down plate", "8 in", "6 in", "10 in", "12 in"),
            T("kyl", "KYL rack", "kyl", "Steel", "Know Your Limits, 5 plates", "5 plates, 2-6 in", "5 plates, 1-3 in", "5 plates, 4-12 in"),
            T("coyote", "Coyote silhouette", "coyote", "Steel", "Varmint silhouette", "Full size", "1/2 size", "1/3 size"),
            T("prairiedog", "Prairie dog", "prairiedog", "Steel", "Small varmint silhouette", "Full size", "1/2 size"),
            T("uspsa", "USPSA / IPSC paper", "uspsa", "Paper", "Metric zones A / C / D", "Full size", "Classic"),
            T("idpa", "IDPA paper", "idpa", "Paper", "-0 / -1 / -3 zones", "Full size"),
            T("noshoot", "No-shoot", "noshoot", "Paper", "White penalty target", "Full size", "Half size"),
            T("bullseye", "Bullseye paper", "bullseye", "Paper", "Ringed paper target", "8.5 x 11 in", "12 x 12 in", "NRA SR-1"),
            T("popper", "Popper", "popper", "Reactive", "Falls when hit", "Full size, 12 x 42 in"),
            T("minipopper", "Mini popper", "minipopper", "Reactive", "2/3 size popper", "8 x 28 in"),
            T("platerack", "Plate rack", "platerack", "Reactive", "Six falling plates", "6 x 8 in plates"),
            T("duelingtree", "Dueling tree", "duelingtree", "Reactive", "Swinging paddles", "6 paddles, 6 in"),
            T("texasstar", "Texas star", "texasstar", "Reactive", "Spinning 5-plate star", "5 x 6 in plates"),
            T("spinner", "Spinner", "spinner", "Reactive", "Paddles that spin over", "2 and 3 in paddles", "3 and 4 in paddles"),
            T("swinger", "Swinger", "swinger", "Reactive", "Pendulum moving target", "8 in plate", "12 in plate"),
            T("custom_target", "Other target", "custom", "Steel", "Anything else", "Custom")
        };

        public static readonly List<PropType> Props = new List<PropType>
        {
            P("barricade", "PRS barricade", "Stepped shooting shelves"),
            P("vtac", "VTAC barricade", "Wall with assorted ports"),
            P("tanktrap", "Tank trap", "Welded steel hedgehog"),
            P("rooftop", "Rooftop", "Pitched roof section"),
            P("ladder", "Ladder", "Leaning rungs"),
            P("tripod", "Tripod", "Rifle tripod and saddle"),
            P("barrel", "Barrel", "55-gallon drum"),
            P("barrelstack", "Barrel stack", "Stacked drums"),
            P("tire", "Tire", "Truck or skidder tire"),
            P("sawhorse", "Sawhorse", "Wooden sawhorse"),
            P("spool", "Cable spool", "Wooden wire reel"),
            P("vehicle", "Vehicle", "Car or truck"),
            P("wallport", "Wall with window", "Window-height port"),
            P("lowport", "Low port", "Opening near the ground"),
            P("table", "Table", "Table start or rest"),
            P("shootingbox", "Shooting box", "Fault-line box"),
            P("gate", "Pipe gate", "Cattle gate rails"),
            P("rock", "Rock", "Boulder"),
            P("log", "Log", "Downed log"),
            P("pronemat", "Prone mat", "Shooting mat"),
            P("bag", "Support bag", "Game changer style bag"),
            P("custom_prop", "Other prop", "Name it yourself")
        };

        static TargetType T(string id, string name, string shape, string category, string hint, params string[] sizes)
        {
            return new TargetType { Id = id, Name = name, Shape = shape, Category = category, Hint = hint, Sizes = sizes };
        }

        static PropType P(string id, string name, string hint)
        {
            return new PropType { Id = id, Name = name, Hint = hint };
        }

        public static TargetType Target(string id)
        {
            return Targets.FirstOrDefault(t => t.Id == id) ?? Targets.Last();
        }

        public static PropType Prop(string id)
        {
            return Props.FirstOrDefault(p => p.Id == id) ?? Props.Last();
        }

        public static bool HasProp(string id)
        {
            return Props.Any(p => p.Id == id);
        }
    }

    class StageIdea
    {
        public string Name, Text;
        public string[] Props;
    }

    static class Defaults
    {
        public static RangeSetup Range(string discipline)
        {
            RangeSetup r;
            if (discipline == Disciplines.Pistol)
            {
                r = new RangeSetup
                {
                    Targets = new List<StageTarget>
                    {
                        StageTarget.Of("uspsa", 7),
                        StageTarget.Of("uspsa", 10),
                        StageTarget.Of("uspsa", 12, null, "Partially covered by a no-shoot"),
                        StageTarget.Of("minipopper", 15),
                        StageTarget.Of("popper", 20),
                        StageTarget.Of("ipsc_full", 25),
                        StageTarget.Of("platerack", 10),
                        StageTarget.Of("texasstar", 12)
                    },
                    PropIds = new List<string> { "vtac", "barrelstack", "wallport", "shootingbox", "table", "lowport" },
                    Positions = new List<string> { "Box A", "Box B", "Window position", "Kneeling port", "Open shooting area" },
                    MinTargets = 4, MaxTargets = 8, MinHits = 2, MaxHits = 2, MinTime = 25, MaxTime = 60,
                    MinProps = 1, MaxProps = 2, PositionCount = 3,
                    RequireMovement = true, SwitchSecondary = true
                };
            }
            else if (discipline == Disciplines.Rimfire)
            {
                r = new RangeSetup
                {
                    Targets = new List<StageTarget>
                    {
                        StageTarget.Of("bullseye", 25),
                        StageTarget.Of("gong", 50, "2 in"),
                        StageTarget.Of("square", 50, "4 in"),
                        StageTarget.Of("kyl", 75, "5 plates, 1-3 in"),
                        StageTarget.Of("spinner", 75),
                        StageTarget.Of("prairiedog", 100),
                        StageTarget.Of("gong", 100, "4 in")
                    },
                    PropIds = new List<string> { "barricade", "tire", "barrel", "ladder", "sawhorse", "tripod", "pronemat", "bag" },
                    Positions = new List<string> { "Standing supported", "Kneeling", "Seated", "Prone", "Barricade" },
                    MinTargets = 3, MaxTargets = 5, MinHits = 1, MaxHits = 2, MinTime = 90, MaxTime = 120,
                    MinProps = 1, MaxProps = 2, PositionCount = 2,
                    RequireMovement = true
                };
            }
            else
            {
                r = new RangeSetup
                {
                    Targets = new List<StageTarget>
                    {
                        StageTarget.Of("gong", 250, "10 in"),
                        StageTarget.Of("ipsc_23", 385),
                        StageTarget.Of("square", 515, "12 in"),
                        StageTarget.Of("ipsc_full", 640),
                        StageTarget.Of("gong", 780, "18 in")
                    },
                    PropIds = new List<string> { "tanktrap", "rooftop", "barricade", "ladder", "tripod", "pronemat" },
                    Positions = new List<string>
                    {
                        "Position 1 - standing supported",
                        "Position 2 - kneeling supported",
                        "Position 3 - seated supported",
                        "Position 4 - prone",
                        "Position 5 - support-side alternate"
                    },
                    MinTargets = 3, MaxTargets = 5, MinHits = 1, MaxHits = 2, MinTime = 120, MaxTime = 120,
                    MinProps = 1, MaxProps = 2, PositionCount = 2,
                    RequireMovement = true
                };
            }
            r.Discipline = discipline;
            r.Normalize();
            return r;
        }

        // The sample briefs from the Course of Fire website.
        public static List<Stage> SampleStages()
        {
            var a = new Stage
            {
                Name = "Near-to-far rooftop",
                Targets = Hits(2, StageTarget.Of("gong", 325, "10 in"), StageTarget.Of("square", 415, "8 in"), StageTarget.Of("ipsc_23", 515)),
                Props = new List<StageProp> { StageProp.Of("rooftop") },
                Positions = new List<string> { "Front edge", "Left slope" },
                ParTime = 120,
                Start = "Standing, rifle staged safe",
                Procedure = "Engage near-to-far from position one, then reverse order from position two."
            };
            var b = new Stage
            {
                Name = "Barricade to prone cleanup",
                Targets = Hits(2, StageTarget.Of("gong", 250, "12 in"), StageTarget.Of("square", 385, "10 in"), StageTarget.Of("ipsc_23", 640)),
                Props = new List<StageProp> { StageProp.Of("barricade"), StageProp.Of("pronemat") },
                Positions = new List<string> { "Window opening", "Prone" },
                ParTime = 105,
                Start = Disciplines.DefaultStart(Disciplines.Rifle),
                Procedure = "Engage two hits each on all targets.",
                Notes = "Farthest target may only be engaged from prone."
            };
            var c = new Stage
            {
                Name = "Ladder support-side test",
                Targets = Hits(2, StageTarget.Of("gong", 300, "10 in"), StageTarget.Of("square", 470, "10 in"),
                    StageTarget.Of("ipsc_23", 580), StageTarget.Of("ipsc_full", 710)),
                Props = new List<StageProp> { StageProp.Of("ladder") },
                Positions = new List<string> { "Upper rung", "Lower rung" },
                ParTime = 120,
                Start = Disciplines.DefaultStart(Disciplines.Rifle),
                Procedure = "Engage each target with two hits from either rung.",
                Notes = "At least one position must be shot support-side."
            };
            return new List<Stage> { a, b, c };
        }

        static List<StageTarget> Hits(int hits, params StageTarget[] targets)
        {
            foreach (var t in targets) t.Hits = hits;
            return targets.ToList();
        }

        // The PRS stage ideas from the Course of Fire website, used as stage templates.
        public static readonly StageIdea[] Ideas =
        {
            Idea("Ladder and tank trap switch",
                "Start on a ladder, engage two near targets and one far target, then move to a tank trap for a second string with changed support and target order.",
                "ladder", "tanktrap"),
            Idea("Three-position rooftop sweep",
                "Use the front, side, and rear faces of a rooftop prop. Limit total rounds, require one position change, and keep par time tight enough to reward smooth transitions.",
                "rooftop"),
            Idea("Barricade to prone reset",
                "Engage from a barricade opening first, then drop to prone to clean up the farthest plate. Good for building stability changes under time.",
                "barricade", "pronemat"),
            Idea("Support-side ladder problem",
                "Force at least one support-side position on a ladder or window frame so the stage rewards positional problem solving instead of pure speed.",
                "ladder"),
            Idea("Near-far-near target rhythm",
                "Alternate distance bands to break up easy cadence. Practice quick position confirmation and hold transitions.",
                "barricade"),
            Idea("Seated start to prop finish",
                "Start seated or hands-off-rifle, then move into a supported position on a barrel or tank trap. Builds stage-entry discipline.",
                "barrel", "tanktrap"),
            Idea("Reverse target order on the second string",
                "Keep the same targets but reverse the order after a position change. Adds complexity without requiring extra steel.",
                "barricade"),
            Idea("Small target priority stage",
                "Put the toughest target in the middle of the stage rather than first or last so the shooter has to manage time and wobble deliberately.",
                "tripod")
        };

        static StageIdea Idea(string name, string text, params string[] props)
        {
            return new StageIdea { Name = name, Text = text, Props = props };
        }
    }
}
