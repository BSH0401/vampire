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
        public int Grade = 1;
        public bool Locked, Sold;

        public bool IsWeapon => Weapon;
        public string Name => IsWeapon ? WeaponGrades.Name(Weapon, Grade) : Chip.displayName;
    }

    /// <summary>
    /// 코어 작업실 상점 (기획서 6.2): 4 random offers (5 with the 확장 진열대 upgrade), reroll with rising cost,
    /// lock to keep an offer for the next visit, sell weapons for 70% of what was paid,
    /// upgrade or merge weapons (등급 I~IV), and trade with the occasional 암시장.
    /// </summary>
    public class Shop
    {
        public const int MaxSlots = 5;
        public const float SellRatio = 0.7f;
        const float PriceGrowthPerWave = 0.12f;

        readonly ShopDatabase db;
        readonly Loadout loadout;
        readonly GameManager gm;
        int wave, rerollStep, visitsWithoutMarket;

        /// <summary>One entry per visible slot (4, or 5 with the 확장 진열대 upgrade).</summary>
        public readonly ShopOffer[] Offers;
        public int SlotCount => Offers.Length;
        public int RerollCost { get; private set; }

        /// <summary>The 암시장 is open during this visit only.</summary>
        public bool MarketOpen { get; private set; }
        public readonly List<MarketOffer> MarketOffers = new();

        public event Action Changed;
        /// <summary>Something was bought from the 암시장 (achievement).</summary>
        public event Action MarketPurchase;

        public Shop(ShopDatabase db, Loadout loadout, GameManager gm)
        {
            this.db = db;
            this.loadout = loadout;
            this.gm = gm;
            Offers = new ShopOffer[Mathf.Clamp(MetaProgress.ShopSlots, 1, MaxSlots)];
        }

        /// <param name="upcomingWave">The wave the player is preparing for; drives prices and tiers.</param>
        public void Open(int upcomingWave)
        {
            wave = upcomingWave;
            rerollStep = 1 + wave / 5;
            RerollCost = Mathf.Max(0, 1 + wave / 2 - MetaProgress.RerollDiscount);
            Roll();
            // Locked offers carry over at their original price, but the lock is spent.
            foreach (var o in Offers) o.Locked = false;
            RollMarket();
            Changed?.Invoke();
        }

        // ─────────────── 암시장 ───────────────

        void RollMarket()
        {
            MarketOffers.Clear();
            bool eligible = wave >= BlackMarket.FirstWave;
            MarketOpen = DevCommandLine.ForceBlackMarket ||
                         (eligible && (UnityEngine.Random.value < BlackMarket.Chance || visitsWithoutMarket + 1 >= BlackMarket.PityVisits));
            if (eligible) visitsWithoutMarket = MarketOpen ? 0 : visitsWithoutMarket + 1;
            if (!MarketOpen) return;

            var chips = new List<ChipData>(BlackMarket.Catalog);
            // One illegal modification when there is a weapon to modify, the rest forbidden chips.
            var targets = loadout.Weapons.FindAll(w => !w.MaxGrade);
            if (targets.Count > 0)
            {
                var t = targets[UnityEngine.Random.Range(0, targets.Count)];
                MarketOffers.Add(new MarketOffer { Target = t, Price = Mathf.Max(1, Mathf.RoundToInt(UpgradeCost(t) * BlackMarket.ModPriceRatio)) });
            }
            while (MarketOffers.Count < BlackMarket.OfferCount && chips.Count > 0)
            {
                int i = UnityEngine.Random.Range(0, chips.Count);
                MarketOffers.Add(new MarketOffer { Chip = chips[i], Price = ScalePrice(chips[i].price) });
                chips.RemoveAt(i);
            }
        }

        public string MarketBlockReason(int index)
        {
            var o = MarketOffers[index];
            if (o.Sold) return "거래 완료";
            if (o.IsMod && (!loadout.Weapons.Contains(o.Target) || o.Target.MaxGrade)) return "대상 없음";
            if (gm.Scrap < o.Price) return "스크랩 부족";
            return null;
        }

        public bool BuyMarket(int index)
        {
            if (index < 0 || index >= MarketOffers.Count || MarketBlockReason(index) != null) return false;
            var o = MarketOffers[index];
            if (!gm.TrySpendScrap(o.Price)) return false;
            o.Sold = true;
            if (o.IsMod)
            {
                loadout.RaiseGrade(loadout.Weapons.IndexOf(o.Target), o.Price);
                loadout.AddRunModifier(StatType.MaxHP, -BlackMarket.ModHpCost);
            }
            else loadout.AddChip(o.Chip);
            MarketPurchase?.Invoke();
            Changed?.Invoke();
            return true;
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
                var w = Pick(db.weapons, x => x.tier <= maxTier && MetaProgress.IsUnlocked(x)) ?? db.weapons[0];
                int grade = RollGrade();
                return new ShopOffer { Weapon = w, Grade = grade, Price = ScalePrice(w.price * WeaponGrades.OfferPriceMultiplier(grade)) };
            }
            var c = Pick(db.chips, x => x.tier <= maxTier && MetaProgress.IsUnlocked(x)) ?? db.chips[0];
            return new ShopOffer { Chip = c, Price = ScalePrice(c.price) };
        }

        static T Pick<T>(List<T> list, Predicate<T> allowed) where T : class
        {
            var options = list.FindAll(x => x != null && allowed(x));
            return options.Count > 0 ? options[UnityEngine.Random.Range(0, options.Count)] : null;
        }

        /// <summary>Higher-grade weapons start showing up in the second half of the run.</summary>
        int RollGrade()
        {
            float r = UnityEngine.Random.value;
            float p4 = Mathf.Clamp((wave - 16) * 0.04f, 0f, 0.12f);
            float p3 = Mathf.Clamp((wave - 11) * 0.04f, 0f, 0.2f);
            float p2 = Mathf.Clamp((wave - 5) * 0.05f, 0f, 0.35f);
            if (r < p4) return 4;
            if (r < p4 + p3) return 3;
            return r < p4 + p3 + p2 ? 2 : 1;
        }

        int ScalePrice(float basePrice) =>
            Mathf.Max(1, Mathf.RoundToInt(basePrice * (1f + PriceGrowthPerWave * (wave - 1)) * Cores.Current.ShopPriceMultiplier));

        public string BlockReason(int index)
        {
            var o = Offers[index];
            if (o == null || o.Sold) return "판매됨";
            if (o.IsWeapon && loadout.WeaponSlotsFull && loadout.MergeTargetFor(o.Weapon, o.Grade) < 0) return "슬롯 가득";
            if (gm.Scrap < o.Price) return "스크랩 부족";
            return null;
        }

        public bool Buy(int index)
        {
            if (BlockReason(index) != null) return false;
            var o = Offers[index];
            if (!gm.TrySpendScrap(o.Price)) return false;
            if (o.IsWeapon)
            {
                // Full slots: a copy at the same grade merges straight into the owned weapon.
                if (loadout.WeaponSlotsFull) loadout.RaiseGrade(loadout.MergeTargetFor(o.Weapon, o.Grade), o.Price);
                else loadout.AddWeapon(o.Weapon, o.Price, o.Grade);
            }
            else loadout.AddChip(o.Chip);
            o.Sold = true;
            o.Locked = false;
            Changed?.Invoke();
            return true;
        }

        /// <summary>True when buying this weapon offer merges it into an owned copy instead of taking a slot.</summary>
        public bool MergesOnBuy(int index)
        {
            var o = Offers[index];
            return o != null && o.IsWeapon && !o.Sold && loadout.WeaponSlotsFull && loadout.MergeTargetFor(o.Weapon, o.Grade) >= 0;
        }

        // ─────────────── 무기 강화 · 합치기 ───────────────

        /// <returns>Scrap cost to raise the weapon one grade, or -1 at max grade.</returns>
        public int UpgradeCost(OwnedWeapon w)
        {
            if (w == null || w.MaxGrade) return -1;
            return ScalePrice(w.Data.price * WeaponGrades.UpgradePriceMultiplier(w.Grade) * Cores.Current.UpgradeCostMultiplier);
        }

        public int UpgradeCost(int weaponIndex) => weaponIndex < loadout.Weapons.Count ? UpgradeCost(loadout.Weapons[weaponIndex]) : -1;

        public bool Upgrade(int weaponIndex)
        {
            int cost = UpgradeCost(weaponIndex);
            if (cost < 0 || !gm.TrySpendScrap(cost)) return false;
            loadout.RaiseGrade(weaponIndex, cost);
            Changed?.Invoke();
            return true;
        }

        public bool Merge(int weaponIndex)
        {
            if (!loadout.Merge(weaponIndex)) return false;
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
