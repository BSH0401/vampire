using UnityEngine;

namespace CoreOverclock
{
    /// <summary>
    /// First-run hints that teach the heat / vent loop (the game's USP) in context.
    /// Shown until the player clears wave 3 once; stored in PlayerPrefs (never written by test runs).
    /// </summary>
    public class Tutorial
    {
        const string DoneKey = "tutorial_done";

        readonly HUD hud;
        bool active, heatHintShown, overclockHintShown, meltdownHintShown;

        public Tutorial(HUD hud)
        {
            this.hud = hud;
            active = PlayerPrefs.GetInt(DoneKey, 0) == 0 || DevCommandLine.Enabled;
        }

        public void OnWaveStart(int wave)
        {
            if (!active) return;
            switch (wave)
            {
                case 1:
                    hud.ShowHint("이동: WASD / 방향키 / 좌스틱  ·  공격은 자동으로 가장 가까운 적을 조준합니다", 6f);
                    break;
                case 2:
                    hud.ShowHint("적이 떨군 스크랩(◆)으로 웨이브 사이 코어 작업실에서 무기와 칩셋을 구매하세요", 6f);
                    break;
                case 4:
                    if (!DevCommandLine.Enabled)
                    {
                        PlayerPrefs.SetInt(DoneKey, 1);
                        PlayerPrefs.Save();
                    }
                    active = false;
                    break;
            }
        }

        public void Tick(GameManager gm)
        {
            if (!active) return;
            var heat = gm.Heat;
            if (!heatHintShown && heat.Value > 40f)
            {
                heatHintShown = true;
                hud.ShowHint("무기를 쏠수록 열(HEAT)이 오릅니다  ·  70% 이상: 오버클럭(공격력↑ 탄속↑)  ·  100%: 과열(5초 무기 정지)", 7f);
            }
            else if (!overclockHintShown && heat.State == HeatState.Overclock)
            {
                overclockHintShown = true;
                hud.ShowHint("오버클럭 중!  위험해지면 Space / 우클릭 / A로 긴급 방열 — 열 50% 방출 + 주변 적 밀쳐내기", 7f);
            }
            else if (!meltdownHintShown && heat.State == HeatState.Meltdown)
            {
                meltdownHintShown = true;
                hud.ShowHint("과열! 냉각 무기(크라이오)나 히트싱크 칩셋으로 발열을 관리해 보세요", 7f);
            }
        }
    }
}
