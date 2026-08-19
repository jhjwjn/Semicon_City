using System;
using UnityEngine;

namespace SemiconCity.Game
{
    public enum SemiconWorkerKind
    {
        None,
        Mina,
        Rex,
        Bo7
    }

    public enum SemiconRobotKind
    {
        None,
        Bolt01,
        Swift02,
        Gauge03,
        Mule04,
        Pico05,
        Forge06,
        Vector07,
        Sentry08,
        Orbit09,
        Prism10,
        Titan11,
        Helix12,
        Aurora13,
        Nano14,
        Zenith15
    }

    public enum SemiconRobotRarity
    {
        N,
        R,
        SR
    }

    public enum SemiconDiskKind
    {
        None,
        Production,
        Speed,
        Quality
    }

    public enum SemiconDiskGrade
    {
        None,
        I,
        II,
        III
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
        // Kept only so save files made before the robot conversion can migrate safely.
        public SemiconWorkerKind worker;
        public SemiconRobotKind robot;
        public SemiconDiskKind disk;
        public SemiconDiskGrade diskGrade;
        public SemiconRobotKind[] robots;
        public int[] robotEnhancements;
        public SemiconDiskKind[] disks;
        public SemiconDiskGrade[] diskGrades;
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
            robot = SemiconRobotKind.None;
            disk = SemiconDiskKind.None;
            diskGrade = SemiconDiskGrade.None;
            EnsureCrewSlots();
            ClearJob();
        }

        public void EnsureCrewSlots()
        {
            const int count = SemiconFactoryDefinitions.RobotsPerSlot;
            if (robots == null || robots.Length != count)
            {
                var previous = robots;
                robots = new SemiconRobotKind[count];
                if (previous != null) Array.Copy(previous, robots, Math.Min(previous.Length, count));
            }
            if (robotEnhancements == null || robotEnhancements.Length != count)
            {
                var previous = robotEnhancements;
                robotEnhancements = new int[count];
                if (previous != null) Array.Copy(previous, robotEnhancements, Math.Min(previous.Length, count));
            }
            if (disks == null || disks.Length != count)
            {
                var previous = disks;
                disks = new SemiconDiskKind[count];
                if (previous != null) Array.Copy(previous, disks, Math.Min(previous.Length, count));
            }
            if (diskGrades == null || diskGrades.Length != count)
            {
                var previous = diskGrades;
                diskGrades = new SemiconDiskGrade[count];
                if (previous != null) Array.Copy(previous, diskGrades, Math.Min(previous.Length, count));
            }

            if (robots[0] == SemiconRobotKind.None && robot != SemiconRobotKind.None) robots[0] = robot;
            if (disks[0] == SemiconDiskKind.None && disk != SemiconDiskKind.None)
            {
                disks[0] = disk;
                diskGrades[0] = diskGrade == SemiconDiskGrade.None ? SemiconDiskGrade.II : diskGrade;
            }
            SyncLegacyCrewSlot();
        }

        public void SyncLegacyCrewSlot()
        {
            if (robots == null || robots.Length == 0) return;
            robot = robots[0];
            disk = disks != null && disks.Length > 0 ? disks[0] : SemiconDiskKind.None;
            diskGrade = diskGrades != null && diskGrades.Length > 0 && disk != SemiconDiskKind.None
                ? diskGrades[0]
                : SemiconDiskGrade.None;
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

    public readonly struct SemiconRobotDefinition
    {
        public readonly SemiconRobotKind Kind;
        public readonly SemiconRobotRarity Rarity;
        public readonly string Code;
        public readonly string Name;
        public readonly string Role;
        public readonly int Production;
        public readonly int Speed;
        public readonly int Quality;

        public SemiconRobotDefinition(SemiconRobotKind kind, SemiconRobotRarity rarity, string code, string name,
            string role, int production, int speed, int quality)
        {
            Kind = kind;
            Rarity = rarity;
            Code = code;
            Name = name;
            Role = role;
            Production = production;
            Speed = speed;
            Quality = quality;
        }
    }

    public readonly struct SemiconGachaReward
    {
        public readonly bool IsRobot;
        public readonly SemiconRobotKind Robot;
        public readonly SemiconDiskKind Disk;
        public readonly SemiconDiskGrade Grade;
        public readonly int RobotEnhancement;
        public readonly bool UpgradeTriggered;

        public SemiconGachaReward(SemiconRobotKind robot, int enhancement = 0, bool upgradeTriggered = false)
        {
            IsRobot = true;
            Robot = robot;
            Disk = SemiconDiskKind.None;
            Grade = SemiconDiskGrade.None;
            RobotEnhancement = enhancement;
            UpgradeTriggered = upgradeTriggered;
        }

        public SemiconGachaReward(SemiconDiskKind disk, SemiconDiskGrade grade)
        {
            IsRobot = false;
            Robot = SemiconRobotKind.None;
            Disk = disk;
            Grade = grade;
            RobotEnhancement = 0;
            UpgradeTriggered = false;
        }
    }

    public static class SemiconFactoryDefinitions
    {
        public const int SlotCount = 3;
        public const int RobotsPerSlot = 3;
        public const int MaxRobotEnhancement = 5;
        public const int EnhancementMergeCount = 3;
        public const float EnhancementBonusPerLevel = 0.2f;
        public const int MachineInstallPrice = 3500;
        public const int RobotSingleDrawPrice = 1500;
        public const int RobotTenDrawPrice = 13500;
        public const int DiskSingleDrawPrice = 700;
        public const int DiskTenDrawPrice = 6300;
        public const float WaferCycleSeconds = 6f;
        public const float OxidationCycleSeconds = 7f;
        public const float PhotoCycleSeconds = 7.5f;
        public const float EtchCycleSeconds = 8f;
        public const float DepositionCycleSeconds = 8.5f;
        public const float MetalCycleSeconds = 9f;
        public const float EdsCycleSeconds = 9.5f;
        public const float PackageCycleSeconds = 10f;

        private static readonly SemiconRobotDefinition[] Robots =
        {
            new(SemiconRobotKind.Bolt01, SemiconRobotRarity.N, "BOLT-01", "볼트", "자재 운반", 8, 2, 2),
            new(SemiconRobotKind.Swift02, SemiconRobotRarity.N, "SWIFT-02", "스위프트", "고속 조립", 2, 8, 2),
            new(SemiconRobotKind.Gauge03, SemiconRobotRarity.N, "GAUGE-03", "게이지", "기초 검사", 2, 2, 8),
            new(SemiconRobotKind.Mule04, SemiconRobotRarity.N, "MULE-04", "뮬", "라인 물류", 6, 5, 1),
            new(SemiconRobotKind.Pico05, SemiconRobotRarity.N, "PICO-05", "피코", "미세 정비", 1, 6, 5),
            new(SemiconRobotKind.Forge06, SemiconRobotRarity.R, "FORGE-06", "포지", "생산 증폭", 12, 5, 3),
            new(SemiconRobotKind.Vector07, SemiconRobotRarity.R, "VECTOR-07", "벡터", "동선 최적화", 4, 13, 3),
            new(SemiconRobotKind.Sentry08, SemiconRobotRarity.R, "SENTRY-08", "센트리", "정밀 검수", 3, 4, 13),
            new(SemiconRobotKind.Orbit09, SemiconRobotRarity.R, "ORBIT-09", "오비트", "웨이퍼 핸들링", 8, 9, 5),
            new(SemiconRobotKind.Prism10, SemiconRobotRarity.R, "PRISM-10", "프리즘", "계측 보정", 5, 7, 10),
            new(SemiconRobotKind.Titan11, SemiconRobotRarity.SR, "TITAN-11", "타이탄", "중량 공정", 16, 8, 6),
            new(SemiconRobotKind.Helix12, SemiconRobotRarity.SR, "HELIX-12", "헬릭스", "초고속 자동화", 7, 17, 6),
            new(SemiconRobotKind.Aurora13, SemiconRobotRarity.SR, "AURORA-13", "오로라", "결함 분석", 6, 8, 17),
            new(SemiconRobotKind.Nano14, SemiconRobotRarity.SR, "NANO-14", "나노", "나노 공정", 12, 12, 10),
            new(SemiconRobotKind.Zenith15, SemiconRobotRarity.SR, "ZENITH-15", "제니스", "전 공정 지휘", 14, 13, 13)
        };

        public static int RobotCount => Robots.Length;

        public static SemiconRobotDefinition GetRobot(SemiconRobotKind kind)
        {
            var index = (int)kind - 1;
            return index >= 0 && index < Robots.Length
                ? Robots[index]
                : new SemiconRobotDefinition(SemiconRobotKind.None, SemiconRobotRarity.N, "--", "미배정",
                    "기본 설비 운전", 0, 0, 0);
        }

        public static SemiconRobotKind GetRobotByCatalogIndex(int index)
        {
            return index >= 0 && index < Robots.Length ? Robots[index].Kind : SemiconRobotKind.None;
        }

        public static SemiconProductionStats GetStats(SemiconRobotKind robot, SemiconDiskKind disk,
            SemiconDiskGrade grade)
        {
            return GetStats(robot, 0, disk, grade);
        }

        public static SemiconProductionStats GetStats(SemiconRobotKind robot, int enhancement,
            SemiconDiskKind disk, SemiconDiskGrade grade)
        {
            var definition = GetRobot(robot);
            var multiplier = 1f + Mathf.Clamp(enhancement, 0, MaxRobotEnhancement) * EnhancementBonusPerLevel;
            var production = 100 + (int)Math.Round(definition.Production * multiplier);
            var speed = 100 + (int)Math.Round(definition.Speed * multiplier);
            var quality = 80 + (int)Math.Round(definition.Quality * multiplier);
            var diskBonus = GetDiskValue(grade);

            switch (disk)
            {
                case SemiconDiskKind.Production:
                    production += diskBonus;
                    break;
                case SemiconDiskKind.Speed:
                    speed += diskBonus;
                    break;
                case SemiconDiskKind.Quality:
                    quality += diskBonus;
                    break;
            }

            return new SemiconProductionStats(production, speed, quality);
        }

        public static SemiconProductionStats GetStats(SemiconFactorySlotData slot)
        {
            if (slot == null) return GetStats(SemiconRobotKind.None, SemiconDiskKind.None, SemiconDiskGrade.None);
            slot.EnsureCrewSlots();
            var production = 100;
            var speed = 100;
            var quality = 80;
            for (var index = 0; index < RobotsPerSlot; index++)
            {
                var definition = GetRobot(slot.robots[index]);
                var multiplier = 1f + Mathf.Clamp(slot.robotEnhancements[index], 0, MaxRobotEnhancement) *
                    EnhancementBonusPerLevel;
                production += (int)Math.Round(definition.Production * multiplier);
                speed += (int)Math.Round(definition.Speed * multiplier);
                quality += (int)Math.Round(definition.Quality * multiplier);
                var diskBonus = GetDiskValue(slot.diskGrades[index]);
                switch (slot.disks[index])
                {
                    case SemiconDiskKind.Production: production += diskBonus; break;
                    case SemiconDiskKind.Speed: speed += diskBonus; break;
                    case SemiconDiskKind.Quality: quality += diskBonus; break;
                }
            }
            return new SemiconProductionStats(production, speed, quality);
        }

        public static SemiconProductionStats GetStats(SemiconWorkerKind worker, SemiconDiskKind disk)
        {
            var robot = worker switch
            {
                SemiconWorkerKind.Mina => SemiconRobotKind.Nano14,
                SemiconWorkerKind.Rex => SemiconRobotKind.Helix12,
                SemiconWorkerKind.Bo7 => SemiconRobotKind.Aurora13,
                _ => SemiconRobotKind.None
            };
            return GetStats(robot, disk, disk == SemiconDiskKind.None ? SemiconDiskGrade.None : SemiconDiskGrade.II);
        }

        public static string GetRobotName(SemiconRobotKind robot)
        {
            if (robot == SemiconRobotKind.None) return "미배정";
            var definition = GetRobot(robot);
            return $"{definition.Code}  /  {definition.Name}";
        }

        public static string GetRobotBonus(SemiconRobotKind robot)
        {
            return GetRobotBonus(robot, 0);
        }

        public static string GetRobotBonus(SemiconRobotKind robot, int enhancement)
        {
            if (robot == SemiconRobotKind.None) return "기본 설비 성능으로 가동";
            var definition = GetRobot(robot);
            var multiplier = 1f + Mathf.Clamp(enhancement, 0, MaxRobotEnhancement) * EnhancementBonusPerLevel;
            return $"생산 +{Math.Round(definition.Production * multiplier)}  ·  " +
                   $"속도 +{Math.Round(definition.Speed * multiplier)}  ·  " +
                   $"품질 +{Math.Round(definition.Quality * multiplier)}";
        }

        public static string GetRobotEnhancementText(int enhancement)
        {
            return enhancement <= 0 ? "기본" : $"+{Mathf.Clamp(enhancement, 0, MaxRobotEnhancement)}강";
        }

        public static string GetRobotRole(SemiconRobotKind robot) => GetRobot(robot).Role;

        public static string GetRobotRarityText(SemiconRobotKind robot) => GetRobot(robot).Rarity.ToString();

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

        public static string GetDiskName(SemiconDiskKind disk, SemiconDiskGrade grade)
        {
            return disk == SemiconDiskKind.None || grade == SemiconDiskGrade.None
                ? "미장착"
                : $"{GetDiskName(disk)}  {grade}";
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

        public static int GetDiskValue(SemiconDiskGrade grade)
        {
            return grade switch
            {
                SemiconDiskGrade.I => 6,
                SemiconDiskGrade.II => 12,
                SemiconDiskGrade.III => 20,
                _ => 0
            };
        }

        public static string GetDiskBonus(SemiconDiskKind disk, SemiconDiskGrade grade)
        {
            if (disk == SemiconDiskKind.None || grade == SemiconDiskGrade.None) return "디스크 슬롯 비어 있음";
            var label = disk switch
            {
                SemiconDiskKind.Production => "생산",
                SemiconDiskKind.Speed => "속도",
                SemiconDiskKind.Quality => "품질",
                _ => "효과"
            };
            return $"{label} +{GetDiskValue(grade)}";
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
