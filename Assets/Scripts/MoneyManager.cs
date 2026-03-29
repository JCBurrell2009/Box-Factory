using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public int money;

    public void SetFunds(int funds) => money = funds;

    public void AddFunds(int addedAmount) => money += addedAmount;
}
