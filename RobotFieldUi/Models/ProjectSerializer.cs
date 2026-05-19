using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RobotFieldUi.Models.TreePanel;

namespace RobotFieldUi.Models;

public static class ProjectSerializer
{
    // ── DTOs ──────────────────────────────────────────────────────────────────

    private record PointDto(double X, double Y, string Seg, double ArcR, string ArcD, bool IsCenter = false);
    private record PathDto(string Name, List<PointDto> Points);
    private record MissionDto(string Name, List<PathDto> Paths);
    private record ProjectDto(string Name, List<MissionDto> Missions);
    private record FileDto(int Version, List<ProjectDto> Projects);

    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    // ── Serialize ─────────────────────────────────────────────────────────────

    public static string Serialize(IEnumerable<Project> projects)
    {
        var dto = new FileDto(1, projects.Select(ToDto).ToList());
        return JsonSerializer.Serialize(dto, Opts);
    }

    private static ProjectDto  ToDto(Project p)     => new(p.Name, p.Missions.Select(ToDto).ToList());
    private static MissionDto  ToDto(Mission m)     => new(m.Name, m.Paths.Select(ToDto).ToList());
    private static PathDto     ToDto(MissionPath p) => new(p.Name, p.Points.Select(ToDto).ToList());
    private static PointDto    ToDto(PathPoint pt)  =>
        new(pt.X, pt.Y, pt.SegmentOut.ToString(), pt.ArcRadius, pt.ArcDirection.ToString(), pt.IsCenter);

    // ── Deserialize ───────────────────────────────────────────────────────────

    public static List<Project> Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<FileDto>(json, Opts)
            ?? throw new FormatException("Nieprawidłowy format pliku.");
        return dto.Projects.Select(FromDto).ToList();
    }

    private static Project FromDto(ProjectDto dto)
    {
        var p = new Project(dto.Name);
        foreach (var m in dto.Missions) p.Missions.Add(FromDto(m));
        return p;
    }

    private static Mission FromDto(MissionDto dto)
    {
        var m = new Mission(dto.Name);
        foreach (var path in dto.Paths) m.Paths.Add(FromDto(path));
        return m;
    }

    private static MissionPath FromDto(PathDto dto)
    {
        var path = new MissionPath(dto.Name);
        foreach (var pt in dto.Points) path.Points.Add(FromDto(pt));
        return path;
    }

    private static PathPoint FromDto(PointDto dto)
    {
        var pt = new PathPoint(dto.X, dto.Y);
        if (Enum.TryParse<SegmentType>(dto.Seg, out var seg)) pt.SegmentOut = seg;
        pt.ArcRadius = dto.ArcR;
        if (Enum.TryParse<ArcDirection>(dto.ArcD, out var dir)) pt.ArcDirection = dir;
        pt.IsCenter = dto.IsCenter;
        return pt;
    }
}
