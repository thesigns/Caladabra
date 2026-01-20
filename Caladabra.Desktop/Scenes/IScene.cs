using SFML.Graphics;
using SFML.Window;

namespace Caladabra.Desktop.Scenes;

public interface IScene
{
    void Enter();
    void Exit();
    void HandleEvent(Event sfmlEvent);
    void Update(float deltaTime);
    void Render(RenderWindow window);
}
