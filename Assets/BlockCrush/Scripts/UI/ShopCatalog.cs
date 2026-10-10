using System;
using UnityEngine;

namespace BlockCrush.UI
{
    [Serializable]
    public sealed class ShopOffer
    {
        public string id, title, price;
        public int coins, bombs, boosters, reverses;
        public bool bundle, removeAds;
        public bool Valid => !string.IsNullOrEmpty(id) && coins >= 0 && bombs >= 0 && boosters >= 0 && reverses >= 0 &&
            (removeAds || coins > 0 || bombs > 0 || boosters > 0 || reverses > 0);
        public string Contents => removeAds ? "Remove Ads (demo entitlement)" : bundle ?
            coins + " coins · " + bombs + " bombs · " + boosters + " boosters · " + reverses + " reverse" :
            coins > 0 ? coins + " coins" : boosters + " booster";
    }
    [CreateAssetMenu(menuName = "Block Crush/Shop")]
    public sealed class ShopCatalog : ScriptableObject
    {
        public ShopOffer[] offers = {
            Bundle("starter","STARTER BUNDLES"), Bundle("extra","EXTRA BUNDLES"), Bundle("ultra","ULTRA BUNDLES"),
            Bundle("value","VALUE BUNDLES"), Bundle("special","SPECIAL BUNDLES"),
            new ShopOffer { id="remove_ads",title="REMOVE ADS",price="$ 4.99",removeAds=true },
            new ShopOffer { id="booster_1",title="X1 Booster",price="$ 4.99",boosters=1 },
            new ShopOffer { id="coins_1000",title="1000 Coins",price="$ 0.99",coins=1000 },
            new ShopOffer { id="coins_15000",title="15000 Coins",price="$ 9.99",coins=15000 },
            new ShopOffer { id="coins_25000",title="25000 Coins",price="$ 14.99",coins=25000 },
            new ShopOffer { id="coins_2000",title="2000 Coins",price="119.000 ₫",coins=2000 }
        };
        static ShopOffer Bundle(string id, string title) => new ShopOffer { id=id,title=title,price="$ 4.99",bundle=true,coins=4000,bombs=10,boosters=10,reverses=10 };
        public bool Valid
        {
            get
            {
                if (offers == null || offers.Length != 11) return false;
                var ids = new System.Collections.Generic.HashSet<string>();
                foreach (var offer in offers) if (offer == null || !offer.Valid || !ids.Add(offer.id)) return false;
                return true;
            }
        }
    }
}
