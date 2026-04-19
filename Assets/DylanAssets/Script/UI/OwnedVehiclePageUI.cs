using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OwnedVehiclesPageUI : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private TMP_Text moneyText;

    [Header("Layout")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private OwnedVehicleCardUI cardPrefab;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private TMP_Text emptyStateText;

    [Header("Feedback")]
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TerminalManagerUI terminalManagerUI;

    private readonly List<OwnedVehicleCardUI> spawnedCards = new List<OwnedVehicleCardUI>();

    private void OnEnable()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        ClearCards();
        ClearError();
        RefreshMoney();

        if (DbBoot.Instance == null)
            return;

        if (contentRoot == null || cardPrefab == null)
        {
            SetError("Owned vehicles page is not wired correctly.");
            return;
        }

        var db = DbBoot.Instance.Db;
        var vehicles = VehicleService.GetOwnedVehicles();

        // SHOW EMPTY MESSAGE
        if (vehicles.Count == 0)
        {
            if (emptyState != null)
                emptyState.SetActive(true);

            if (emptyStateText != null)
                emptyStateText.text = "No vehicles currently owned";

            return;
        }

        // HIDE EMPTY MESSAGE
        if (emptyState != null)
            emptyState.SetActive(false);

        foreach (var vehicle in vehicles)
        {
            var type = db.Find<VehicleType>(vehicle.vehicleTypeId);

            var card = Instantiate(cardPrefab, contentRoot);
            card.Setup(vehicle, type, OnVehicleSold);

            spawnedCards.Add(card);
        }
    }

    private void OnVehicleSold(string message)
    {
        RefreshMoney();

        if (terminalManagerUI != null)
            terminalManagerUI.SetStatusMessage(message);

        Rebuild();
    }

    private void RefreshMoney()
    {
        if (moneyText == null || DbBoot.Instance == null)
            return;

        var player = PlayerService.Get();
        if (player != null)
            moneyText.text = $"Money: €{player.money:0}";
    }

    private void ClearCards()
    {
        for (int i = spawnedCards.Count - 1; i >= 0; i--)
        {
            if (spawnedCards[i] != null)
                Destroy(spawnedCards[i].gameObject);
        }

        spawnedCards.Clear();

        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);
    }

    private void SetError(string message)
    {
        if (errorText != null)
            errorText.text = message;

        //debug.LogError(message);
    }

    private void ClearError()
    {
        if (errorText != null)
            errorText.text = "";
    }
}