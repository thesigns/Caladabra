using SFML.Graphics;
using SFML.Window;

namespace Caladabra.Desktop.Scenes;

public sealed class SceneManager
{
    private readonly Stack<IScene> _sceneStack = new();

    public IScene? CurrentScene => _sceneStack.Count > 0 ? _sceneStack.Peek() : null;

    public bool HasScenes => _sceneStack.Count > 0;

    public void PushScene(IScene scene)
    {
        CurrentScene?.Exit();
        _sceneStack.Push(scene);
        scene.Enter();
    }

    public void PopScene()
    {
        if (_sceneStack.Count == 0) return;

        var scene = _sceneStack.Pop();
        scene.Exit();
        CurrentScene?.Enter();
    }

    public void ReplaceScene(IScene scene)
    {
        if (_sceneStack.Count > 0)
        {
            var old = _sceneStack.Pop();
            old.Exit();
        }
        _sceneStack.Push(scene);
        scene.Enter();
    }

    public void HandleEvent(Event sfmlEvent)
    {
        CurrentScene?.HandleEvent(sfmlEvent);
    }

    public void Update(float deltaTime)
    {
        CurrentScene?.Update(deltaTime);
    }

    public void Render(RenderWindow window)
    {
        // Render all scenes from bottom to top (for overlay support)
        var scenes = _sceneStack.Reverse().ToArray();
        foreach (var scene in scenes)
        {
            scene.Render(window);
        }
    }
}
