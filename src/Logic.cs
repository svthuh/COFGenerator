using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace CourseOfFire
{
    // The library lives in one JSON file; each save keeps the previous version as a backup.
    static class Store
    {
        public static readonly string Folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CourseOfFire");
        public static readonly string LibraryPath = Path.Combine(Folder, "library.json");
        static readonly string BackupPath = Path.Combine(Folder, "library.backup.json");

        public static string LoadWarning;

        public static Library Load()
        {
            LoadWarning = null;
            foreach (var path in new[] { LibraryPath, BackupPath })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var lib = Read<Library>(path);
                    if (lib == null) continue;
                    lib.Normalize();
                    if (path == BackupPath)
                        LoadWarning = "Your library file was damaged, so Course of Fire loaded the most recent backup.";
                    return lib;
                }
                catch (Exception)
                {
                    try { File.Copy(path, path + ".damaged-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), true); } catch { }
                }
            }

            var fresh = new Library();
            fresh.Normalize();
            fresh.Stages.AddRange(Defaults.SampleStages());
            return fresh;
        }

        public static void Save(Library lib)
        {
            Directory.CreateDirectory(Folder);
            string temp = LibraryPath + ".saving";
            Write(temp, lib);
            if (File.Exists(LibraryPath)) File.Replace(temp, LibraryPath, BackupPath, true);
            else File.Move(temp, LibraryPath);
        }

        public static T Read<T>(string path)
        {
            using (var stream = File.OpenRead(path))
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
        }

        public static void Write<T>(string path, T value)
        {
            using (var stream = File.Create(path))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true, "  "))
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(writer, value);
                writer.Flush();
            }
        }

        public static void ExportCourseFile(string path, IEnumerable<Stage> stages)
        {
            Write(path, new CourseFile { Format = "course-of-fire", Version = 1, Stages = stages.ToList() });
        }

        // Reads a .cof file (or a whole library file) and returns its stages.
        public static List<Stage> ImportCourseFile(string path)
        {
            List<Stage> stages = null;
            try
            {
                var file = Read<CourseFile>(path);
                if (file != null) stages = file.Stages;
            }
            catch (Exception) { }
            if (stages == null)
            {
                var lib = Read<Library>(path);
                if (lib != null) stages = lib.Stages;
            }
            if (stages == null) throw new InvalidDataException("This file doesn't contain any courses of fire.");
            stages.RemoveAll(s => s == null);
            foreach (var s in stages) s.Normalize();
            return stages;
        }
    }

    static class Generator
    {
        static readonly Random Rng = new Random();

        public static Stage Generate(RangeSetup range, out string error)
        {
            error = null;
            range.Normalize();
            var targets = range.Targets.ToList();
            var positions = range.Positions.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
            if (targets.Count == 0) { error = "Add at least one target to your range first."; return null; }
            if (positions.Count == 0) { error = "Add at least one position first."; return null; }

            int hi = Math.Min(range.MaxTargets, targets.Count);
            int lo = Math.Min(range.MinTargets, hi);
            var picked = Sample(targets, RandInt(lo, hi)).Select(t => t.Clone()).ToList();
            if (range.NearToFar) picked = picked.OrderBy(t => t.DistanceInYards).ToList();
            int hits = RandInt(range.MinHits, range.MaxHits);
            foreach (var t in picked) t.Hits = hits;

            int propHi = Math.Min(range.MaxProps, range.PropIds.Count);
            int propLo = Math.Min(range.MinProps, propHi);
            var props = Sample(range.PropIds, RandInt(propLo, propHi)).Select(id => StageProp.Of(id)).ToList();

            var stage = new Stage
            {
                Name = "Random " + ShortName(range.Discipline) + " stage",
                Discipline = range.Discipline,
                Targets = picked,
                Props = props,
                Positions = Sample(positions, Math.Min(range.PositionCount, positions.Count)),
                ParTime = RandInt(range.MinTime, range.MaxTime),
                Start = Disciplines.DefaultStart(range.Discipline)
            };

            var notes = new List<string>();
            if (range.RequireMovement && stage.Positions.Count > 1)
                notes.Add("Movement between at least two positions is required.");
            if (range.SupportSide)
                notes.Add("At least one string must be fired support-side.");
            if (range.SwitchSecondary && range.Discipline == Disciplines.Pistol)
                notes.Add("At some point in the stage, transition to the secondary weapon if it is safe and appropriate for the event.");
            if (range.NearToFar)
                notes.Add("Engage targets in the order listed, near to far.");
            notes.Add(range.Discipline == Disciplines.Pistol
                ? "Confirm bay layout, muzzle-safe movement paths, and match rules before use."
                : "Confirm target visibility, safe firing angles, and realistic movement before use.");
            stage.Notes = string.Join(Environment.NewLine, notes);
            stage.Procedure = WriteProcedure(stage);
            return stage;
        }

        public static string WriteProcedure(Stage s)
        {
            var sb = new StringBuilder("On the start signal, ");
            sb.Append(s.Positions.Count > 1 ? "from the " + s.Positions.Count + " listed positions, " : "from the listed position, ");
            if (s.Targets.Count == 0)
            {
                sb.Append("engage the targets");
            }
            else
            {
                int hits = s.Targets[0].Hits;
                bool same = s.Targets.All(t => t.Hits == hits);
                sb.Append("engage ").Append(s.Targets.Count == 1 ? "the target" : "each target");
                sb.Append(same ? " with " + hits + (hits == 1 ? " hit" : " hits") : " for the number of hits listed");
            }
            sb.Append(" within ").Append(s.ParTime).Append(" seconds.");
            if (s.Targets.Count > 0) sb.Append(" ").Append(s.RoundCount).Append(" rounds minimum.");
            return sb.ToString();
        }

        static string ShortName(string discipline)
        {
            if (discipline == Disciplines.Pistol) return "pistol / carbine";
            if (discipline == Disciplines.Rimfire) return "rimfire";
            return "rifle";
        }

        static int RandInt(int min, int max)
        {
            if (max < min) max = min;
            return Rng.Next(min, max + 1);
        }

        static List<T> Sample<T>(IList<T> list, int count)
        {
            var pool = list.ToList();
            var picked = new List<T>();
            while (pool.Count > 0 && picked.Count < count)
            {
                int i = Rng.Next(pool.Count);
                picked.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return picked;
        }
    }

    static class TextExport
    {
        public static string TargetLine(StageTarget t)
        {
            var type = Catalog.Target(t.TypeId);
            var parts = new List<string> { type.Name };
            if (!string.IsNullOrEmpty(t.Size) && t.Size != "Custom") parts[0] += " (" + t.Size + ")";
            if (t.Distance > 0) parts.Add(t.DistanceText);
            parts.Add(t.Hits + (t.Hits == 1 ? " hit" : " hits"));
            if (!string.IsNullOrEmpty(t.Notes)) parts.Add(t.Notes);
            return string.Join(" - ", parts);
        }

        public static string Summary(Stage s)
        {
            return s.Discipline + " · " + s.Targets.Count + (s.Targets.Count == 1 ? " target" : " targets") +
                   " · " + s.RoundCount + " rounds · " + s.ParTime + " s par time";
        }

        public static string ToText(Stage s)
        {
            var sb = new StringBuilder();
            sb.AppendLine(s.Name.ToUpperInvariant());
            sb.AppendLine(Summary(s));
            sb.AppendLine();
            sb.AppendLine("TARGETS");
            for (int i = 0; i < s.Targets.Count; i++)
                sb.AppendLine("  T" + (i + 1) + "  " + TargetLine(s.Targets[i]));
            if (s.Props.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("PROPS AND OBSTACLES");
                foreach (var p in s.Props) sb.AppendLine("  - " + p.DisplayName);
            }
            if (s.Positions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("POSITIONS");
                for (int i = 0; i < s.Positions.Count; i++) sb.AppendLine("  " + (i + 1) + ". " + s.Positions[i]);
            }
            AppendBlock(sb, "START", s.Start);
            AppendBlock(sb, "PROCEDURE", s.Procedure);
            AppendBlock(sb, "NOTES", s.Notes);
            return sb.ToString();
        }

        static void AppendBlock(StringBuilder sb, string title, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            sb.AppendLine();
            sb.AppendLine(title);
            foreach (var line in text.Split('\n')) sb.AppendLine("  " + line.TrimEnd('\r'));
        }

        public static string ToCsv(IEnumerable<Stage> stages)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name,Discipline,Targets,Target details,Rounds,Par time (s),Props,Positions,Start,Procedure,Notes,Last edited");
            foreach (var s in stages)
            {
                var cells = new[]
                {
                    s.Name, s.Discipline, s.Targets.Count.ToString(),
                    string.Join("; ", s.Targets.Select((t, i) => "T" + (i + 1) + " " + TargetLine(t))),
                    s.RoundCount.ToString(), s.ParTime.ToString(),
                    string.Join("; ", s.Props.Select(p => p.DisplayName)),
                    string.Join("; ", s.Positions),
                    s.Start, s.Procedure, s.Notes,
                    s.ModifiedTime.ToString("yyyy-MM-dd HH:mm")
                };
                sb.AppendLine(string.Join(",", cells.Select(c => Csv(c))));
            }
            return sb.ToString();
        }

        static string Csv(string value)
        {
            value = (value ?? "").Replace("\r\n", " / ").Replace("\n", " / ");
            return value.IndexOfAny(new[] { ',', '"' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
    }
}
