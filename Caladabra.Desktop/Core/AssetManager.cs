using System.Reflection;
using SFML.Graphics;

namespace Caladabra.Desktop.Core;

public sealed class AssetManager
{
    private readonly Dictionary<string, Font> _fonts = new();
    private readonly Dictionary<string, Texture> _textures = new();

    private readonly string _basePath;
    private readonly string _fontsPath;
    private readonly string _texturesPath;

    public AssetManager()
    {
        // Get path relative to executable location
        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        _basePath = Path.Combine(exeDir, "Assets");
        _fontsPath = Path.Combine(_basePath, "Fonts");
        _texturesPath = Path.Combine(_basePath, "Textures");
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

    public Texture GetTexture(string path)
    {
        if (_textures.TryGetValue(path, out var texture))
            return texture;

        var fullPath = Path.Combine(_texturesPath, path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Texture not found: {fullPath}");

        texture = new Texture(fullPath);
        _textures[path] = texture;
        return texture;
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
