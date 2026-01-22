using System.Reflection;
using Caladabra.Core.Cards;
using SFML.Graphics;

namespace Caladabra.Desktop.Core;

public sealed class AssetManager
{
    private readonly Dictionary<string, Font> _fonts = new();
    private readonly Dictionary<string, Texture> _textures = new();

    private readonly string _basePath;
    private readonly string _fontsPath;

    public AssetManager()
    {
        // Get path relative to executable location
        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        _basePath = Path.Combine(exeDir, "Assets");
        _fontsPath = Path.Combine(_basePath, "Fonts");
    }

    public Font GetFont(string name)
    {
        if (_fonts.TryGetValue(name, out var font))
            return font;

        var path = Path.Combine(_fontsPath, name);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Font not found: {path}");

        font = new Font(path);
        _fonts[name] = font;
        return font;
    }

    public Font DefaultFont => GetFont("Lato-Regular.ttf");

    public Texture GetTexture(string relativePath)
    {
        if (_textures.TryGetValue(relativePath, out var texture))
            return texture;

        var fullPath = Path.Combine(_basePath, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Texture not found: {fullPath}");

        texture = new Texture(fullPath);
        texture.Smooth = true;  // Bilinear filtering - wygładza krawędzie
        _textures[relativePath] = texture;
        return texture;
    }

    // === Card textures ===

    public Texture GetCardFront(Flavor flavor) =>
        GetTexture(Path.Combine("Cards", "Fronts", $"card_front_{flavor.ToString().ToLower()}.png"));

    public Texture GetCardBack(Flavor flavor) =>
        GetTexture(Path.Combine("Cards", "Backs", $"card_back_{flavor.ToString().ToLower()}.png"));

    public Texture GetFlavorIcon(Flavor flavor) =>
        GetTexture(Path.Combine("Cards", "Icons", $"icon_{flavor.ToString().ToLower()}.png"));

    public Texture? GetCardArt(string cardId)
    {
        var path = Path.Combine("Cards", "Art", $"{cardId}.png");
        var fullPath = Path.Combine(_basePath, path);
        if (!File.Exists(fullPath))
            return null;
        return GetTexture(path);
    }

    public void PreloadAssets()
    {
        // Preload default font
        _ = DefaultFont;
    }

    public void Dispose()
    {
        foreach (var font in _fonts.Values)
            font.Dispose();
        _fonts.Clear();

        foreach (var texture in _textures.Values)
            texture.Dispose();
        _textures.Clear();
    }
}
