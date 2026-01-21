using System.Text.Json;
using System.Text.Json.Serialization;
using Caladabra.Core.Cards;
using Caladabra.Core.Effects;
using Caladabra.Core.Engine;
using Caladabra.Core.Zones;

namespace Caladabra.Core.State;

/// <summary>
/// Pełny stan gry Caladabra.
/// </summary>
public sealed class GameState
{
    // === Zasoby gracza ===

    /// <summary>Aktualny Tłuszcz (cel: zredukować do 0).</summary>
    public int Fat { get; set; } = GameRules.StartingFat;

    /// <summary>Aktualna Siła Woli.</summary>
    public int Willpower { get; set; } = GameRules.StartingWillpower;

    /// <summary>Numer aktualnej tury.</summary>
    public int Turn { get; set; } = 1;

    /// <summary>Seed użyty do tasowania talii (null = custom deck bez tasowania).</summary>
    public int? Seed { get; set; }

    // === Faza gry ===

    /// <summary>Aktualna faza gry.</summary>
    public GamePhase Phase { get; set; } = GamePhase.AwaitingAction;

    /// <summary>Oczekująca decyzja gracza (jeśli Phase == AwaitingChoice).</summary>
    [JsonIgnore]
    public PendingChoice? PendingChoice { get; set; }

    // === Modyfikatory ===

    /// <summary>Aktywne modyfikatory (od kart na stole).</summary>
    public List<ActiveModifier> ActiveModifiers { get; } = [];

    // === Strefy ===

    /// <summary>Lista Kart Caladabra - metastrefa ze wszystkimi kartami.</summary>
    [JsonIgnore]
    public CardList CardList { get; } = new();

    /// <summary>Spiżarnia - talia kart.</summary>
    public Pantry Pantry { get; } = new();

    /// <summary>Ręka gracza.</summary>
    public Hand Hand { get; } = new();

    /// <summary>Stół - karty z efektami trwałymi.</summary>
    public Table Table { get; } = new();

    /// <summary>Żołądek - zjedzone karty.</summary>
    public Stomach Stomach { get; } = new();

    /// <summary>Kibelek - karty odrzucone.</summary>
    public Toilet Toilet { get; } = new();

    // === Metody pomocnicze ===

    /// <summary>
    /// Modyfikuje Tłuszcz z zachowaniem limitów.
    /// </summary>
    public int ModifyFat(int delta)
    {
        int oldValue = Fat;
        Fat = Math.Max(GameRules.MinFat, Fat + delta);
        return Fat - oldValue; // Zwraca faktyczną zmianę
    }

    /// <summary>
    /// Modyfikuje Siłę Woli z zachowaniem limitów.
    /// </summary>
    public int ModifyWillpower(int delta)
    {
        int oldValue = Willpower;
        Willpower = Math.Clamp(Willpower + delta, GameRules.MinWillpower, GameRules.MaxWillpower);
        return Willpower - oldValue; // Zwraca faktyczną zmianę
    }

    /// <summary>
    /// Sprawdza warunek wygranej.
    /// </summary>
    public bool CheckWinCondition() => Fat <= GameRules.MinFat;

    /// <summary>
    /// Sprawdza warunek przegranej.
    /// </summary>
    public bool CheckLoseCondition() => Hand.Count == 0 && Pantry.Count == 0;

    /// <summary>
    /// Pobiera strefę po typie.
    /// </summary>
    public IZone GetZone(ZoneType zoneType) => zoneType switch
    {
        ZoneType.CardList => CardList,
        ZoneType.Pantry => Pantry,
        ZoneType.Hand => Hand,
        ZoneType.Table => Table,
        ZoneType.Stomach => Stomach,
        ZoneType.Toilet => Toilet,
        _ => throw new ArgumentOutOfRangeException(nameof(zoneType))
    };
}
