using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreOverclock
{
    public class ShopOffer
    {
        public WeaponData Weapon;
        public ChipData Chip;
        public int Price;
        public bool Locked, Sold;

        public bool IsWeapon => Weapon;
        public string Name => IsWeapon ? Weapon.displayName : Chip.displayName;
    }

    /// <summary>
    /// 코어 작업실 상점 (기획서 6.2): 4 random offers, reroll with rising cost,
    /// lock to keep an offer for the next visit, sell weapons for 70% of what was paid.
    /// </summary>
    public class Shop
    {
        public const int SlotCount = 4;
        public const float SellRatio = 0.7f;
        const float PriceGrowthPerWave = 0.1f;

        readonly ShopDatabase db;
        readonly Loadout loadout;
        readonly GameManager gm;
        int wave, rerollStep;

        public readonly ShopOffer[] Offers = new ShopOffer[SlotCount];
        public int RerollCost { get; private set; }

        public event Action Changed;

        public Shop(ShopDatabase db, Loadout loadout, GameManager gm)
        {
            this.db = db;
            this.loadout = loadout;
            this.gm = gm;
        }

        /// <param name="upcomingWave">The wave the player is preparing for; drives prices and tiers.</param>
        public void Open(int upcomingWave)
        {
            wave = upcomingWave;
            rerollStep = 1 + wave / 5;
            RerollCost = 1 + wave / 2;
            Roll();
            // Locked offers carry over at their original price, but the lock is spent.
            foreach (var o in Offers) o.Locked = false;
            Changed?.Invoke();
        }

        public bool Reroll()
        {
            if (!gm.TrySpendScrap(RerollCost)) return false;
            RerollCost += rerollStep;
            Roll();
            Changed?.Invoke();
            return true;
        }

        void Roll()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                var o = Offers[i];
                if (o != null && o.Locked && !o.Sold) continue;
                Offers[i] = RandomOffer();
            }
        }

        ShopOffer RandomOffer()
        {
            int maxTier = 1 + (wave - 1) / 4;
            bool weapon = db.chips.Count == 0 || (db.weapons.Count > 0 && UnityEngine.Random.value < db.weaponChance);
            if (weapon)
            {
                var w = Pick(db.weapons, x => x.tier <= maxTier) ?? db.weapons[0];
                return new ShopOffer { Weapon = w, Price = ScalePrice(w.price) };
            }
            var c = Pick(db.chips, x => x.tier <= maxTier) ?? db.chips[0];
            return new ShopOffer { Chip = c, Price = ScalePrice(c.price) };
        }

        static T Pick<T>(List<T> list, Predicate<T> allowed) where T : class
        {
            var options = list.FindAll(x => x != null && allowed(x));
            return options.Count > 0 ? options[UnityEngine.Random.Range(0, options.Count)] : null;
        }

        int ScalePrice(int basePrice) => Mathf.Max(1, Mathf.RoundToInt(basePrice * (1f + PriceGrowthPerWave * (wave - 1))));

        public string BlockReason(int index)
        {
            var o = Offers[index];
            if (o == null || o.Sold) return "판매됨";
            if (o.IsWeapon && loadout.WeaponSlotsFull) return "슬롯 가득";
            if (gm.Scrap < o.Price) return "스크랩 부족";
            return null;
        }

        public bool Buy(int index)
        {
            if (BlockReason(index) != null) return false;
            var o = Offers[index];
            if (!gm.TrySpendScrap(o.Price)) return false;
            if (o.IsWeapon) loadout.AddWeapon(o.Weapon, o.Price);
            else loadout.AddChip(o.Chip);
            o.Sold = true;
            o.Locked = false;
            Changed?.Invoke();
            return true;
        }

        public void ToggleLock(int index)
        {
            var o = Offers[index];
            if (o == null || o.Sold) return;
            o.Locked = !o.Locked;
            Changed?.Invoke();
        }

        public static int SellValue(OwnedWeapon w) => Mathf.Max(1, Mathf.RoundToInt(w.PaidPrice * SellRatio));

        /// <summary>재활용: the last weapon cannot be sold so the next wave stays playable.</summary>
        public bool CanSell(int weaponIndex) => weaponIndex < loadout.Weapons.Count && loadout.Weapons.Count > 1;

        public bool Sell(int weaponIndex)
        {
            if (!CanSell(weaponIndex)) return false;
            gm.AddScrap(SellValue(loadout.Weapons[weaponIndex]));
            loadout.RemoveWeaponAt(weaponIndex);
            Changed?.Invoke();
            return true;
        }
    }
}
