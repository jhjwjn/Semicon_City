using System;

namespace SemiconCity.Game
{
    public enum SemiconWorkerKind
    {
        None,
        Mina,
        Rex,
        Bo7
    }

    public enum SemiconDiskKind
    {
        None,
        Production,
        Speed,
        Quality
    }

    public enum SemiconRecipeKind
    {
        None,
        WaferSubstrate,
        Sc01ControlSensor,
        OxidizedWafer,
        PhotoPatternedWafer,
        EtchedWafer,
        DepositedWafer,
        MetalizedWafer,
        TestedWafer,
        Pm10PowerManagement,
        Dd20DisplayDriver
    }

    [Serializable]
    public sealed class SemiconFactorySlotData
    {
        public bool machineInstalled;
        public SemiconWorkerKind worker;
        public SemiconDiskKind disk;
        public SemiconRecipeKind activeJobRecipe;
        public int activeJobBatches;
        public int activeJobOutput;
        public int activeJobQuality;
        public long activeJobStartUtcTicks;
        public long activeJobFinishUtcTicks;

        public SemiconFactorySlotData(bool installed = false)
        {
            machineInstalled = installed;
            worker = SemiconWorkerKind.None;
            disk = SemiconDiskKind.None;
            ClearJob();
        }

        public void ClearJob()
        {
            activeJobRecipe = SemiconRecipeKind.None;
            activeJobBatches = 0;
            activeJobOutput = 0;
            activeJobQuality = 0;
            activeJobStartUtcTicks = 0;
            activeJobFinishUtcTicks = 0;
        }
    }

    public readonly struct SemiconProductionJobSnapshot
    {
        public readonly bool HasJob;
        public readonly bool IsComplete;
        public readonly SemiconRecipeKind Recipe;
        public readonly int Batches;
        public readonly int OutputAmount;
        public readonly int Quality;
        public readonly float Progress;
        public readonly float RemainingSeconds;
        public readonly float TotalSeconds;

        public SemiconProductionJobSnapshot(bool hasJob, bool complete, SemiconRecipeKind recipe, int batches,
            int outputAmount, int quality, float progress, float remainingSeconds, float totalSeconds)
        {
            HasJob = hasJob;
            IsComplete = complete;
            Recipe = recipe;
            Batches = batches;
            OutputAmount = outputAmount;
            Quality = quality;
            Progress = progress;
            RemainingSeconds = remainingSeconds;
            TotalSeconds = totalSeconds;
        }
    }

    public readonly struct SemiconProductionStats
    {
        public readonly int Production;
        public readonly int Speed;
        public readonly int Quality;
        public readonly int OutputPerCycle;
        public readonly float CycleSeconds;

        public SemiconProductionStats(int production, int speed, int quality)
        {
            Production = production;
            Speed = speed;
            Quality = quality;
            OutputPerCycle = production >= 120 ? 2 : 1;
            CycleSeconds = 8f * 100f / Math.Max(1, speed);
        }
    }

    public readonly struct SemiconProductionResult
    {
        public readonly int InputCycles;
        public readonly int OutputAmount;
        public readonly int Quality;
        public readonly float CycleSeconds;

        public SemiconProductionResult(int inputCycles, int outputAmount, int quality, float cycleSeconds)
        {
            InputCycles = inputCycles;
            OutputAmount = outputAmount;
            Quality = quality;
            CycleSeconds = cycleSeconds;
        }
    }

    public static class SemiconFactoryDefinitions
    {
        public const int SlotCount = 3;
        public const int MachineInstallPrice = 3500;
        public const float WaferCycleSeconds = 6f;
        public const float OxidationCycleSeconds = 7f;
        public const float PhotoCycleSeconds = 7.5f;
        public const float EtchCycleSeconds = 8f;
        public const float DepositionCycleSeconds = 8.5f;
        public const float MetalCycleSeconds = 9f;
        public const float EdsCycleSeconds = 9.5f;
        public const float PackageCycleSeconds = 10f;

        public static SemiconProductionStats GetStats(SemiconWorkerKind worker, SemiconDiskKind disk)
        {
            var production = 100;
            var speed = 100;
            var quality = 80;

            switch (worker)
            {
                case SemiconWorkerKind.Mina:
                    production += 10;
                    speed += 8;
                    quality += 12;
                    break;
                case SemiconWorkerKind.Rex:
                    production += 5;
                    speed += 16;
                    quality += 6;
                    break;
                case SemiconWorkerKind.Bo7:
                    production += 4;
                    speed += 5;
                    quality += 18;
                    break;
            }

            switch (disk)
            {
                case SemiconDiskKind.Production:
                    production += 12;
                    break;
                case SemiconDiskKind.Speed:
                    speed += 15;
                    break;
                case SemiconDiskKind.Quality:
                    quality += 15;
                    break;
            }

            return new SemiconProductionStats(production, speed, quality);
        }

        public static string GetWorkerName(SemiconWorkerKind worker)
        {
            return worker switch
            {
                SemiconWorkerKind.Mina => "미나  /  공정 엔지니어",
                SemiconWorkerKind.Rex => "렉스  /  자동화 엔지니어",
                SemiconWorkerKind.Bo7 => "BO-7  /  작업 로봇",
                _ => "미배정"
            };
        }

        public static string GetWorkerBonus(SemiconWorkerKind worker)
        {
            return worker switch
            {
                SemiconWorkerKind.Mina => "생산 +10  ·  속도 +8  ·  품질 +12",
                SemiconWorkerKind.Rex => "생산 +5  ·  속도 +16  ·  품질 +6",
                SemiconWorkerKind.Bo7 => "생산 +4  ·  속도 +5  ·  품질 +18",
                _ => "기본 설비 성능으로 가동"
            };
        }

        public static string GetDiskName(SemiconDiskKind disk)
        {
            return disk switch
            {
                SemiconDiskKind.Production => "생산 증폭 디스크",
                SemiconDiskKind.Speed => "오버클럭 디스크",
                SemiconDiskKind.Quality => "계측 보정 디스크",
                _ => "미장착"
            };
        }

        public static string GetDiskBonus(SemiconDiskKind disk)
        {
            return disk switch
            {
                SemiconDiskKind.Production => "생산 +12",
                SemiconDiskKind.Speed => "속도 +15",
                SemiconDiskKind.Quality => "품질 +15",
                _ => "디스크 슬롯 비어 있음"
            };
        }

        public static string GetRecipeName(SemiconRecipeKind recipe)
        {
            return recipe switch
            {
                SemiconRecipeKind.WaferSubstrate => "기초 웨이퍼  /  WAFER-01",
                SemiconRecipeKind.OxidizedWafer => "산화 웨이퍼  /  OXIDE-01",
                SemiconRecipeKind.PhotoPatternedWafer => "패턴 웨이퍼  /  PHOTO-01",
                SemiconRecipeKind.EtchedWafer => "식각 웨이퍼  /  ETCH-01",
                SemiconRecipeKind.DepositedWafer => "박막 웨이퍼  /  DEPO-01",
                SemiconRecipeKind.MetalizedWafer => "배선 웨이퍼  /  METAL-01",
                SemiconRecipeKind.TestedWafer => "선별 웨이퍼  /  EDS-01",
                SemiconRecipeKind.Sc01ControlSensor => "제어 센서 패키지  /  PACKAGE-01",
                SemiconRecipeKind.Pm10PowerManagement => "전력 관리 IC  /  PM-10",
                SemiconRecipeKind.Dd20DisplayDriver => "디스플레이 드라이버  /  DD-20",
                _ => "레시피 미선택"
            };
        }

        public static string GetRecipeDescription(SemiconRecipeKind recipe)
        {
            return recipe switch
            {
                SemiconRecipeKind.WaferSubstrate => "고순도 실리콘을 절단·연마하여 다음 공정에\n투입할 기초 웨이퍼를 제작합니다.",
                SemiconRecipeKind.OxidizedWafer => "확정된 산화 레시피로 기초 웨이퍼 표면에\n균일한 절연 산화막을 성장시킵니다.",
                SemiconRecipeKind.PhotoPatternedWafer => "포토레지스트를 도포한 산화 웨이퍼에\n회로 패턴을 노광·현상합니다.",
                SemiconRecipeKind.EtchedWafer => "패턴 웨이퍼의 노출 영역을 선택적으로 제거해\n회로 구조를 정밀하게 전사합니다.",
                SemiconRecipeKind.DepositedWafer => "식각 구조 위에 균일한 기능성 박막을 형성해\n다음 배선 공정을 준비합니다.",
                SemiconRecipeKind.MetalizedWafer => "박막 웨이퍼 위에 저저항 금속 배선을 형성해\n전기적 연결 구조를 완성합니다.",
                SemiconRecipeKind.TestedWafer => "배선 웨이퍼의 전기 특성을 검사해 불량 다이를\n식별하고 다음 패키징 공정에 전달합니다.",
                SemiconRecipeKind.Sc01ControlSensor => "EDS 선별 웨이퍼를 와이어 본딩하고 몰딩해\n판매 가능한 SC-01 제어 센서로 완성합니다.",
                SemiconRecipeKind.Pm10PowerManagement => "저누설 패키징 조건을 적용해 산업 장비용\nPM-10 전력 관리 IC로 완성합니다.",
                SemiconRecipeKind.Dd20DisplayDriver => "고정밀 패턴 웨이퍼를 전용 본딩 구성으로\nDD-20 디스플레이 드라이버로 완성합니다.",
                _ => "생산할 레시피를 선택하세요."
            };
        }

        public static string GetRecipeCostText(SemiconRecipeKind recipe)
        {
            return recipe switch
            {
                SemiconRecipeKind.WaferSubstrate => "INPUT / 1 CYCLE\n\n고순도 실리콘      2 EA",
                SemiconRecipeKind.OxidizedWafer => "INPUT / 1 CYCLE\n\n기초 웨이퍼         1 EA\n산화 가스             1 EA",
                SemiconRecipeKind.PhotoPatternedWafer => "INPUT / 1 CYCLE\n\n산화 웨이퍼         1 EA\n포토레지스트         1 EA",
                SemiconRecipeKind.EtchedWafer => "INPUT / 1 CYCLE\n\n패턴 웨이퍼         1 EA\n식각 가스             1 EA",
                SemiconRecipeKind.DepositedWafer => "INPUT / 1 CYCLE\n\n식각 웨이퍼         1 EA\n증착 가스             1 EA",
                SemiconRecipeKind.MetalizedWafer => "INPUT / 1 CYCLE\n\n박막 웨이퍼         1 EA\n배선 금속 타깃      1 EA",
                SemiconRecipeKind.TestedWafer => "INPUT / 1 CYCLE\n\n배선 웨이퍼         1 EA\n검사 프로브           REUSE",
                SemiconRecipeKind.Sc01ControlSensor => "INPUT / 1 CYCLE\n\nEDS 선별 웨이퍼     1 EA\n몰딩 컴파운드         1 EA\n본딩 툴               REUSE",
                SemiconRecipeKind.Pm10PowerManagement => "INPUT / 1 CYCLE\n\nEDS 선별 웨이퍼     1 EA\n몰딩 컴파운드         1 EA\n전력 리드프레임       REUSE",
                SemiconRecipeKind.Dd20DisplayDriver => "INPUT / 1 CYCLE\n\nEDS 선별 웨이퍼     1 EA\n몰딩 컴파운드         1 EA\n다채널 본딩 툴        REUSE",
                _ => "INPUT / --"
            };
        }

        public static float GetBaseCycleSeconds(SemiconRecipeKind recipe)
        {
            return recipe switch
            {
                SemiconRecipeKind.WaferSubstrate => WaferCycleSeconds,
                SemiconRecipeKind.OxidizedWafer => OxidationCycleSeconds,
                SemiconRecipeKind.PhotoPatternedWafer => PhotoCycleSeconds,
                SemiconRecipeKind.EtchedWafer => EtchCycleSeconds,
                SemiconRecipeKind.DepositedWafer => DepositionCycleSeconds,
                SemiconRecipeKind.MetalizedWafer => MetalCycleSeconds,
                SemiconRecipeKind.TestedWafer => EdsCycleSeconds,
                SemiconRecipeKind.Sc01ControlSensor => PackageCycleSeconds,
                SemiconRecipeKind.Pm10PowerManagement => PackageCycleSeconds * 1.15f,
                SemiconRecipeKind.Dd20DisplayDriver => PackageCycleSeconds * 1.3f,
                _ => 0f
            };
        }
    }
}
