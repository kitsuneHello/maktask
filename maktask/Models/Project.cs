using System;
using System.Text.Json.Serialization;

namespace maktask.Models;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ThemeColor { get; set; } = "#0078D4";
    public bool IsVisible { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public Windows.UI.Color UIColor => ParseColor(ThemeColor);

    private static Windows.UI.Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Windows.UI.Color.FromArgb(255,
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
        return Windows.UI.Color.FromArgb(255, 0, 120, 212);
    }
}
