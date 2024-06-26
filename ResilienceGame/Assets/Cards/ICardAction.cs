using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICardAction
{
    public void Played(CardPlayer player, Card cardActedUpon, Card card);
    public void Canceled(CardPlayer player, Card cardActedUpon, Card card);
}
