using System;
using System.IO;
using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconGameState : MonoBehaviour
    {
        [Serializable]
        private sealed class SaveData
        {
            public int researchPoints = 120;
            public int credits = 25000;
            public float bestYield;
            public float bestPrecision;
            public int bestDose = 90;
            public float bestFocus = -0.15f;
            public int experimentCount;
            public bool photoRecipeQualified;
            public int oxidationExperimentCount;
            public bool oxidationRecipeQualified;
            public int bestOxidationTemperature = 1000;
            public int bestOxidationTime = 60;
            public float bestOxideThickness;
            public float bestOxideUniformity;
            public int etchExperimentCount;
            public bool etchRecipeQualified;
            public int bestEtchPower = 250;
            public int bestEtchGasFlow = 60;
            public float bestEtchDepth;
            public float bestEtchProfile;
            public int depositionExperimentCount;
            public bool depositionRecipeQualified;
            public int bestDepositionTemperature = 400;
            public int bestDepositionPressure = 6;
            public float bestDepositionThickness;
            public float bestDepositionUniformity;
            public float bestDepositionCoverage;
            public int metalExperimentCount;
            public bool metalRecipeQualified;
            public int bestMetalPower = 250;
            public int bestMetalTime = 60;
            public float bestMetalThickness;
            public float bestMetalResistance;
            public float bestMetalAdhesion;
            public int edsExperimentCount;
            public bool edsRecipeQualified;
            public int bestEdsVoltage = 3;
            public int bestEdsLeakageThreshold = 30;
            public float bestEdsYield;
            public float bestEdsDetection;
            public float bestEdsFalseReject;
            public int packageExperimentCount;
            public bool packageRecipeQualified;
            public int bestPackageBondingForce = 35;
            public int bestPackageMoldingTemperature = 175;
            public float bestPackageBondStrength;
            public float bestPackageIntegrity;
            public float bestPackageFinalPass;
            public bool firstTutorialCompleted;
            public int unlockedProcessCount = 1;
            public bool firstOrderAccepted;
            public bool firstOrderCompleted;
            public int siliconStock;
            public int processGasStock;
            public int chemicalStock;
            public int metalTargetStock;
            public int finishedProductStock;
            public int finishedProductQualityPoints;
            public int pm10Stock;
            public int pm10QualityPoints;
            public int dd20Stock;
            public int dd20QualityPoints;
            public int activeContractId = -1;
            public int[] contractCompletionCounts;
            public int[] lifetimeProduced;
            public int[] bestProducedQuality;
            public int waferStock;
            public int waferQualityPoints;
            public int oxidizedWaferStock;
            public int oxidizedWaferQualityPoints;
            public int patternedWaferStock;
            public int patternedWaferQualityPoints;
            public int etchedWaferStock;
            public int etchedWaferQualityPoints;
            public int depositedWaferStock;
            public int depositedWaferQualityPoints;
            public int metalizedWaferStock;
            public int metalizedWaferQualityPoints;
            public int testedWaferStock;
            public int testedWaferQualityPoints;
            public SemiconFactorySlotData[] factorySlots;
        }

        public static SemiconGameState Instance { get; private set; }

        [SerializeField] private int researchPoints = 120;
        [SerializeField] private int credits = 25000;
        [SerializeField] private float bestYield;
        [SerializeField] private float bestPrecision;
        [SerializeField] private int bestDose = 90;
        [SerializeField] private float bestFocus = -0.15f;
        [SerializeField] private int experimentCount;
        [SerializeField] private bool photoRecipeQualified;
        [SerializeField] private int oxidationExperimentCount;
        [SerializeField] private bool oxidationRecipeQualified;
        [SerializeField] private int bestOxidationTemperature = 1000;
        [SerializeField] private int bestOxidationTime = 60;
        [SerializeField] private float bestOxideThickness;
        [SerializeField] private float bestOxideUniformity;
        [SerializeField] private int etchExperimentCount;
        [SerializeField] private bool etchRecipeQualified;
        [SerializeField] private int bestEtchPower = 250;
        [SerializeField] private int bestEtchGasFlow = 60;
        [SerializeField] private float bestEtchDepth;
        [SerializeField] private float bestEtchProfile;
        [SerializeField] private int depositionExperimentCount;
        [SerializeField] private bool depositionRecipeQualified;
        [SerializeField] private int bestDepositionTemperature = 400;
        [SerializeField] private int bestDepositionPressure = 6;
        [SerializeField] private float bestDepositionThickness;
        [SerializeField] private float bestDepositionUniformity;
        [SerializeField] private float bestDepositionCoverage;
        [SerializeField] private int metalExperimentCount;
        [SerializeField] private bool metalRecipeQualified;
        [SerializeField] private int bestMetalPower = 250;
        [SerializeField] private int bestMetalTime = 60;
        [SerializeField] private float bestMetalThickness;
        [SerializeField] private float bestMetalResistance;
        [SerializeField] private float bestMetalAdhesion;
        [SerializeField] private int edsExperimentCount;
        [SerializeField] private bool edsRecipeQualified;
        [SerializeField] private int bestEdsVoltage = 3;
        [SerializeField] private int bestEdsLeakageThreshold = 30;
        [SerializeField] private float bestEdsYield;
        [SerializeField] private float bestEdsDetection;
        [SerializeField] private float bestEdsFalseReject;
        [SerializeField] private int packageExperimentCount;
        [SerializeField] private bool packageRecipeQualified;
        [SerializeField] private int bestPackageBondingForce = 35;
        [SerializeField] private int bestPackageMoldingTemperature = 175;
        [SerializeField] private float bestPackageBondStrength;
        [SerializeField] private float bestPackageIntegrity;
        [SerializeField] private float bestPackageFinalPass;
        [SerializeField] private bool firstTutorialCompleted;
        [SerializeField, Range(1, 8)] private int unlockedProcessCount = 1;
        [SerializeField] private bool firstOrderAccepted;
        [SerializeField] private bool firstOrderCompleted;
        [SerializeField] private int siliconStock;
        [SerializeField] private int processGasStock;
        [SerializeField] private int chemicalStock;
        [SerializeField] private int metalTargetStock;
        [SerializeField] private int finishedProductStock;
        [SerializeField] private int finishedProductQualityPoints;
        [SerializeField] private int pm10Stock;
        [SerializeField] private int pm10QualityPoints;
        [SerializeField] private int dd20Stock;
        [SerializeField] private int dd20QualityPoints;
        [SerializeField] private int activeContractId = -1;
        [SerializeField] private int[] contractCompletionCounts;
        [SerializeField] private int[] lifetimeProduced;
        [SerializeField] private int[] bestProducedQuality;
        [SerializeField] private int waferStock;
        [SerializeField] private int waferQualityPoints;
        [SerializeField] private int oxidizedWaferStock;
        [SerializeField] private int oxidizedWaferQualityPoints;
        [SerializeField] private int patternedWaferStock;
        [SerializeField] private int patternedWaferQualityPoints;
        [SerializeField] private int etchedWaferStock;
        [SerializeField] private int etchedWaferQualityPoints;
        [SerializeField] private int depositedWaferStock;
        [SerializeField] private int depositedWaferQualityPoints;
        [SerializeField] private int metalizedWaferStock;
        [SerializeField] private int metalizedWaferQualityPoints;
        [SerializeField] private int testedWaferStock;
        [SerializeField] private int testedWaferQualityPoints;
        [SerializeField] private SemiconFactorySlotData[] factorySlots;
        private bool suppressPersistence;

        public event Action StateChanged;

        public int ResearchPoints => researchPoints;
        public int Credits => credits;
        public float BestYield => bestYield;
        public float BestPrecision => bestPrecision;
        public int BestDose => bestDose;
        public float BestFocus => bestFocus;
        public int ExperimentCount => experimentCount;
        public bool PhotoRecipeQualified => photoRecipeQualified;
        public int OxidationExperimentCount => oxidationExperimentCount;
        public bool OxidationRecipeQualified => oxidationRecipeQualified;
        public int BestOxidationTemperature => bestOxidationTemperature;
        public int BestOxidationTime => bestOxidationTime;
        public float BestOxideThickness => bestOxideThickness;
        public float BestOxideUniformity => bestOxideUniformity;
        public int EtchExperimentCount => etchExperimentCount;
        public bool EtchRecipeQualified => etchRecipeQualified;
        public int BestEtchPower => bestEtchPower;
        public int BestEtchGasFlow => bestEtchGasFlow;
        public float BestEtchDepth => bestEtchDepth;
        public float BestEtchProfile => bestEtchProfile;
        public int DepositionExperimentCount => depositionExperimentCount;
        public bool DepositionRecipeQualified => depositionRecipeQualified;
        public int BestDepositionTemperature => bestDepositionTemperature;
        public int BestDepositionPressure => bestDepositionPressure;
        public float BestDepositionThickness => bestDepositionThickness;
        public float BestDepositionUniformity => bestDepositionUniformity;
        public float BestDepositionCoverage => bestDepositionCoverage;
        public int MetalExperimentCount => metalExperimentCount;
        public bool MetalRecipeQualified => metalRecipeQualified;
        public int BestMetalPower => bestMetalPower;
        public int BestMetalTime => bestMetalTime;
        public float BestMetalThickness => bestMetalThickness;
        public float BestMetalResistance => bestMetalResistance;
        public float BestMetalAdhesion => bestMetalAdhesion;
        public int EdsExperimentCount => edsExperimentCount;
        public bool EdsRecipeQualified => edsRecipeQualified;
        public int BestEdsVoltage => bestEdsVoltage;
        public int BestEdsLeakageThreshold => bestEdsLeakageThreshold;
        public float BestEdsYield => bestEdsYield;
        public float BestEdsDetection => bestEdsDetection;
        public float BestEdsFalseReject => bestEdsFalseReject;
        public int PackageExperimentCount => packageExperimentCount;
        public bool PackageRecipeQualified => packageRecipeQualified;
        public int BestPackageBondingForce => bestPackageBondingForce;
        public int BestPackageMoldingTemperature => bestPackageMoldingTemperature;
        public float BestPackageBondStrength => bestPackageBondStrength;
        public float BestPackageIntegrity => bestPackageIntegrity;
        public float BestPackageFinalPass => bestPackageFinalPass;
        public bool FirstTutorialCompleted => firstTutorialCompleted;
        public int UnlockedProcessCount => Mathf.Clamp(unlockedProcessCount, 1, 8);
        public bool FirstOrderAccepted => firstOrderAccepted;
        public bool FirstOrderCompleted => firstOrderCompleted;
        public int SiliconStock => siliconStock;
        public int ProcessGasStock => processGasStock;
        public int ChemicalStock => chemicalStock;
        public int MetalTargetStock => metalTargetStock;
        public int FinishedProductStock => finishedProductStock;
        public int Pm10Stock => pm10Stock;
        public int Dd20Stock => dd20Stock;
        public SemiconContractKind ActiveContract => activeContractId >= 0 &&
                                                     activeContractId < SemiconContractCatalog.Count
            ? (SemiconContractKind)activeContractId
            : SemiconContractKind.None;
        public int WaferStock => waferStock;
        public int AverageWaferQuality => waferStock > 0 ? waferQualityPoints / waferStock : 80;
        public int OxidizedWaferStock => oxidizedWaferStock;
        public int AverageOxidizedWaferQuality => oxidizedWaferStock > 0
            ? oxidizedWaferQualityPoints / oxidizedWaferStock
            : 80;
        public int PatternedWaferStock => patternedWaferStock;
        public int AveragePatternedWaferQuality => patternedWaferStock > 0
            ? patternedWaferQualityPoints / patternedWaferStock
            : 80;
        public int EtchedWaferStock => etchedWaferStock;
        public int AverageEtchedWaferQuality => etchedWaferStock > 0
            ? etchedWaferQualityPoints / etchedWaferStock
            : 80;
        public int DepositedWaferStock => depositedWaferStock;
        public int AverageDepositedWaferQuality => depositedWaferStock > 0
            ? depositedWaferQualityPoints / depositedWaferStock
            : 80;
        public int MetalizedWaferStock => metalizedWaferStock;
        public int AverageMetalizedWaferQuality => metalizedWaferStock > 0
            ? metalizedWaferQualityPoints / metalizedWaferStock
            : 80;
        public int TestedWaferStock => testedWaferStock;
        public int AverageTestedWaferQuality => testedWaferStock > 0
            ? testedWaferQualityPoints / testedWaferStock
            : 80;
        public int AverageFinishedProductQuality => finishedProductStock > 0
            ? finishedProductQualityPoints / finishedProductStock
            : 80;
        public int AveragePm10Quality => pm10Stock > 0 ? pm10QualityPoints / pm10Stock : 80;
        public int AverageDd20Quality => dd20Stock > 0 ? dd20QualityPoints / dd20Stock : 80;

        public const int SiliconUnitPrice = 180;
        public const int ProcessGasUnitPrice = 130;
        public const int ChemicalUnitPrice = 95;
        public const int MetalTargetUnitPrice = 240;
        public const int FinishedProductSalePrice = 4200;
        public const int PackageTestedWaferCost = 1;
        public const int PackageChemicalCost = 1;
        public const int FirstTutorialCreditReward = 1500;
        public const int FirstTutorialResearchReward = 12;
        public const int FirstOrderCreditReward = 9000;
        public const int FirstOrderResearchReward = 30;
        public const int WaferSiliconCost = 2;
        public const int OxidationWaferCost = 1;
        public const int OxidationGasCost = 1;
        public const int PhotoOxidizedWaferCost = 1;
        public const int PhotoChemicalCost = 1;
        public const int EtchPatternedWaferCost = 1;
        public const int EtchGasCost = 1;
        public const int DepositionEtchedWaferCost = 1;
        public const int DepositionGasCost = 1;
        public const int MetalDepositedWaferCost = 1;
        public const int MetalTargetCost = 1;
        public const int EdsMetalizedWaferCost = 1;

        private string SavePath => Path.Combine(Application.persistentDataPath, "semicon_city_save.json");

        private void Awake()
        {
            Application.runInBackground = true;
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            var arguments = Environment.GetCommandLineArgs();
            suppressPersistence = Array.Exists(arguments, argument =>
                argument.StartsWith("--semicon-", StringComparison.Ordinal) &&
                argument.EndsWith("-smoke-test", StringComparison.Ordinal));
            if (!suppressPersistence)
            {
                Load();
            }
            EnsureFactorySlots();
            EnsureProgressArrays();
        }

        public bool TrySpendResearch(int amount)
        {
            if (amount <= 0 || researchPoints < amount)
            {
                return false;
            }

            researchPoints -= amount;
            SaveAndNotify();
            return true;
        }

        public bool CompleteFirstTutorial()
        {
            if (firstTutorialCompleted) return false;
            firstTutorialCompleted = true;
            unlockedProcessCount = Math.Max(unlockedProcessCount, 2);
            credits += FirstTutorialCreditReward;
            researchPoints += FirstTutorialResearchReward;
            SaveAndNotify();
            return true;
        }

        public bool IsProcessUnlocked(int processNumber)
        {
            return processNumber >= 1 && processNumber <= UnlockedProcessCount;
        }

        public bool TryAcceptFirstOrder(out string reason)
        {
            if (firstOrderCompleted)
            {
                reason = "첫 주문은 이미 납품 완료되었습니다.";
                return false;
            }
            if (firstOrderAccepted)
            {
                reason = "첫 주문을 이미 수락했습니다.";
                return false;
            }
            if (!packageRecipeQualified || UnlockedProcessCount < 8)
            {
                reason = "8대 공정과 PACKAGE-01 레시피를 먼저 개방하세요.";
                return false;
            }

            firstOrderAccepted = true;
            SaveAndNotify();
            reason = string.Empty;
            return true;
        }

        public bool TryCompleteFirstOrder(out string reason)
        {
            if (!firstOrderAccepted || firstOrderCompleted)
            {
                reason = firstOrderCompleted ? "첫 주문은 이미 완료되었습니다." : "먼저 첫 주문을 수락하세요.";
                return false;
            }
            if (finishedProductStock < 1)
            {
                reason = "납품할 SC-01 제어 센서 패키지 1개가 필요합니다.";
                return false;
            }

            var qualityRemoved = AverageFinishedProductQuality;
            finishedProductStock--;
            finishedProductQualityPoints = Math.Max(0, finishedProductQualityPoints - qualityRemoved);
            credits += FirstOrderCreditReward;
            researchPoints += FirstOrderResearchReward;
            firstOrderCompleted = true;
            SaveAndNotify();
            reason = string.Empty;
            return true;
        }

        public int GetContractCompletionCount(SemiconContractKind kind)
        {
            EnsureProgressArrays();
            var index = (int)kind;
            return index >= 0 && index < contractCompletionCounts.Length ? contractCompletionCounts[index] : 0;
        }

        public int GetLifetimeProduced(SemiconRecipeKind recipe)
        {
            EnsureProgressArrays();
            var index = (int)recipe;
            return index >= 0 && index < lifetimeProduced.Length ? lifetimeProduced[index] : 0;
        }

        public int GetBestProducedQuality(SemiconRecipeKind recipe)
        {
            EnsureProgressArrays();
            var index = (int)recipe;
            return index >= 0 && index < bestProducedQuality.Length ? bestProducedQuality[index] : 0;
        }

        public int CompletedContractKinds
        {
            get
            {
                EnsureProgressArrays();
                var count = 0;
                foreach (var completions in contractCompletionCounts)
                    if (completions > 0) count++;
                return count;
            }
        }

        public bool IsContractUnlocked(SemiconContractKind kind)
        {
            if (!firstOrderCompleted) return false;
            if (SemiconContractCatalog.IsSample(kind)) return true;
            if (kind == SemiconContractKind.Sc01IndustrialSensor) return true;
            if (kind == SemiconContractKind.Pm10PowerManagement)
                return CompletedSampleContractKinds >= 2;
            if (kind == SemiconContractKind.Dd20DisplayDriver)
                return CompletedSampleContractKinds >= 4 &&
                       GetContractCompletionCount(SemiconContractKind.Pm10PowerManagement) > 0;
            return false;
        }

        public int CompletedSampleContractKinds
        {
            get
            {
                var count = 0;
                for (var index = 0; index <= 5; index++)
                    if (GetContractCompletionCount((SemiconContractKind)index) > 0) count++;
                return count;
            }
        }

        public bool TryAcceptContract(SemiconContractKind kind, out string reason)
        {
            if (ActiveContract != SemiconContractKind.None)
            {
                reason = "진행 중인 계약을 먼저 완료하세요.";
                return false;
            }
            if (!IsContractUnlocked(kind))
            {
                reason = "선행 계약 또는 공정 개방 조건을 충족하지 못했습니다.";
                return false;
            }

            activeContractId = (int)kind;
            SaveAndNotify();
            reason = string.Empty;
            return true;
        }

        public bool TryDeliverActiveContract(out string reason)
        {
            var kind = ActiveContract;
            if (kind == SemiconContractKind.None)
            {
                reason = "수락한 계약이 없습니다.";
                return false;
            }

            var definition = SemiconContractCatalog.Get(kind);
            var stock = GetRecipeOutputStock(definition.RequiredRecipe);
            var quality = GetRecipeAverageQuality(definition.RequiredRecipe);
            if (stock < definition.RequiredAmount)
            {
                reason = $"{definition.Code} 납품 재고가 부족합니다.  {stock} / {definition.RequiredAmount}";
                return false;
            }
            if (quality < definition.MinimumQuality)
            {
                reason = $"평균 품질 {quality}  /  요구 품질 {definition.MinimumQuality}";
                return false;
            }

            RemoveRecipeStock(definition.RequiredRecipe, definition.RequiredAmount, quality);
            credits += definition.CreditReward;
            researchPoints += definition.ResearchReward;
            EnsureProgressArrays();
            contractCompletionCounts[(int)kind]++;
            activeContractId = -1;
            SaveAndNotify();
            reason = string.Empty;
            return true;
        }

        public int GetRecipeAverageQuality(SemiconRecipeKind recipe)
        {
            return recipe switch
            {
                SemiconRecipeKind.WaferSubstrate => AverageWaferQuality,
                SemiconRecipeKind.OxidizedWafer => AverageOxidizedWaferQuality,
                SemiconRecipeKind.PhotoPatternedWafer => AveragePatternedWaferQuality,
                SemiconRecipeKind.EtchedWafer => AverageEtchedWaferQuality,
                SemiconRecipeKind.DepositedWafer => AverageDepositedWaferQuality,
                SemiconRecipeKind.MetalizedWafer => AverageMetalizedWaferQuality,
                SemiconRecipeKind.TestedWafer => AverageTestedWaferQuality,
                SemiconRecipeKind.Sc01ControlSensor => AverageFinishedProductQuality,
                SemiconRecipeKind.Pm10PowerManagement => AveragePm10Quality,
                SemiconRecipeKind.Dd20DisplayDriver => AverageDd20Quality,
                _ => 0
            };
        }

        public void RecordPhotoExperiment(int dose, float focus, float yield, float precision, bool qualified)
        {
            experimentCount++;
            var previousScore = bestYield + bestPrecision;
            var newScore = yield + precision;
            if (newScore > previousScore)
            {
                bestYield = yield;
                bestPrecision = precision;
                bestDose = dose;
                bestFocus = focus;
            }

            photoRecipeQualified |= qualified;
            SaveAndNotify();
        }

        public void RecordOxidationExperiment(int temperature, int processTime, float thickness,
            float uniformity, bool qualified)
        {
            oxidationExperimentCount++;
            var previousScore = bestOxideUniformity - Mathf.Abs(bestOxideThickness - 100f) * 0.35f;
            var newScore = uniformity - Mathf.Abs(thickness - 100f) * 0.35f;
            if (oxidationExperimentCount == 1 || newScore > previousScore)
            {
                bestOxidationTemperature = temperature;
                bestOxidationTime = processTime;
                bestOxideThickness = thickness;
                bestOxideUniformity = uniformity;
            }

            oxidationRecipeQualified |= qualified;
            SaveAndNotify();
        }

        public void RecordEtchExperiment(int power, int gasFlow, float depth, float profile, bool qualified)
        {
            etchExperimentCount++;
            var previousScore = bestEtchProfile - Mathf.Abs(bestEtchDepth - 120f) * 0.35f;
            var newScore = profile - Mathf.Abs(depth - 120f) * 0.35f;
            if (etchExperimentCount == 1 || newScore > previousScore)
            {
                bestEtchPower = power;
                bestEtchGasFlow = gasFlow;
                bestEtchDepth = depth;
                bestEtchProfile = profile;
            }

            etchRecipeQualified |= qualified;
            SaveAndNotify();
        }

        public void RecordDepositionExperiment(int temperature, int pressure, float thickness,
            float uniformity, float coverage, bool qualified)
        {
            depositionExperimentCount++;
            var previousScore = bestDepositionUniformity + bestDepositionCoverage * 0.5f -
                                Mathf.Abs(bestDepositionThickness - 80f) * 0.35f;
            var newScore = uniformity + coverage * 0.5f - Mathf.Abs(thickness - 80f) * 0.35f;
            if (depositionExperimentCount == 1 || newScore > previousScore)
            {
                bestDepositionTemperature = temperature;
                bestDepositionPressure = pressure;
                bestDepositionThickness = thickness;
                bestDepositionUniformity = uniformity;
                bestDepositionCoverage = coverage;
            }

            depositionRecipeQualified |= qualified;
            SaveAndNotify();
        }

        public void RecordMetalExperiment(int power, int processTime, float thickness, float resistance,
            float adhesion, bool qualified)
        {
            metalExperimentCount++;
            var previousScore = bestMetalAdhesion - Mathf.Abs(bestMetalThickness - 450f) * 0.08f -
                                bestMetalResistance * 25f;
            var newScore = adhesion - Mathf.Abs(thickness - 450f) * 0.08f - resistance * 25f;
            if (metalExperimentCount == 1 || newScore > previousScore)
            {
                bestMetalPower = power;
                bestMetalTime = processTime;
                bestMetalThickness = thickness;
                bestMetalResistance = resistance;
                bestMetalAdhesion = adhesion;
            }

            metalRecipeQualified |= qualified;
            SaveAndNotify();
        }

        public void RecordEdsExperiment(int voltage, int leakageThreshold, float yield, float detection,
            float falseReject, bool qualified)
        {
            edsExperimentCount++;
            var previousScore = bestEdsYield + bestEdsDetection * 0.5f - bestEdsFalseReject * 0.5f;
            var newScore = yield + detection * 0.5f - falseReject * 0.5f;
            if (edsExperimentCount == 1 || newScore > previousScore)
            {
                bestEdsVoltage = voltage;
                bestEdsLeakageThreshold = leakageThreshold;
                bestEdsYield = yield;
                bestEdsDetection = detection;
                bestEdsFalseReject = falseReject;
            }

            edsRecipeQualified |= qualified;
            SaveAndNotify();
        }

        public void RecordPackageExperiment(int bondingForce, int moldingTemperature, float bondStrength,
            float packageIntegrity, float finalPass, bool qualified)
        {
            packageExperimentCount++;
            var previousScore = bestPackageFinalPass + bestPackageBondStrength * 0.4f +
                                bestPackageIntegrity * 0.4f;
            var newScore = finalPass + bondStrength * 0.4f + packageIntegrity * 0.4f;
            if (packageExperimentCount == 1 || newScore > previousScore)
            {
                bestPackageBondingForce = bondingForce;
                bestPackageMoldingTemperature = moldingTemperature;
                bestPackageBondStrength = bondStrength;
                bestPackageIntegrity = packageIntegrity;
                bestPackageFinalPass = finalPass;
            }

            packageRecipeQualified |= qualified;
            SaveAndNotify();
        }

        public int GetMaterialStock(SemiconMaterialKind kind)
        {
            return kind switch
            {
                SemiconMaterialKind.Silicon => siliconStock,
                SemiconMaterialKind.ProcessGas => processGasStock,
                SemiconMaterialKind.Chemicals => chemicalStock,
                SemiconMaterialKind.MetalTarget => metalTargetStock,
                _ => 0
            };
        }

        public int GetMaterialUnitPrice(SemiconMaterialKind kind)
        {
            return kind switch
            {
                SemiconMaterialKind.Silicon => SiliconUnitPrice,
                SemiconMaterialKind.ProcessGas => ProcessGasUnitPrice,
                SemiconMaterialKind.Chemicals => ChemicalUnitPrice,
                SemiconMaterialKind.MetalTarget => MetalTargetUnitPrice,
                _ => 0
            };
        }

        public bool TryBuyMaterial(SemiconMaterialKind kind, int amount)
        {
            if (amount <= 0) return false;

            var unitPrice = GetMaterialUnitPrice(kind);
            var totalPrice = unitPrice * amount;
            if (unitPrice <= 0 || credits < totalPrice) return false;

            credits -= totalPrice;
            switch (kind)
            {
                case SemiconMaterialKind.Silicon:
                    siliconStock += amount;
                    break;
                case SemiconMaterialKind.ProcessGas:
                    processGasStock += amount;
                    break;
                case SemiconMaterialKind.Chemicals:
                    chemicalStock += amount;
                    break;
                case SemiconMaterialKind.MetalTarget:
                    metalTargetStock += amount;
                    break;
            }

            SaveAndNotify();
            return true;
        }

        public bool TrySellFinishedProducts(int amount)
        {
            if (amount <= 0 || finishedProductStock < amount) return false;

            var unitPrice = GetFinishedProductSalePrice();
            var qualityRemoved = AverageFinishedProductQuality * amount;
            finishedProductStock -= amount;
            finishedProductQualityPoints = Math.Max(0, finishedProductQualityPoints - qualityRemoved);
            credits += unitPrice * amount;
            SaveAndNotify();
            return true;
        }

        public void AddFinishedProducts(int amount)
        {
            if (amount <= 0) return;
            finishedProductStock += amount;
            finishedProductQualityPoints += 80 * amount;
            SaveAndNotify();
        }

        public bool CanProduceSc01(int amount)
        {
            return CanProduceSc01(amount, 0);
        }

        public bool CanProduceSc01(int amount, int slotIndex)
        {
            var slot = GetFactorySlot(slotIndex);
            return slot != null && slot.machineInstalled && !GetProductionJob(slotIndex).HasJob &&
                   amount > 0 && packageRecipeQualified &&
                   testedWaferStock >= PackageTestedWaferCost * amount &&
                   chemicalStock >= PackageChemicalCost * amount;
        }

        public bool TryProduceSc01(int amount, out string reason)
        {
            return TryProduceSc01(amount, 0, out _, out reason);
        }

        public bool TryProduceSc01(int amount, int slotIndex, out SemiconProductionResult result, out string reason)
        {
            result = default;
            if (!TryStartProduction(slotIndex, SemiconRecipeKind.Sc01ControlSensor, amount,
                    out var job, out reason)) return false;
            result = new SemiconProductionResult(amount, job.OutputAmount, job.Quality, job.TotalSeconds);
            return true;
        }

        public SemiconProductionJobSnapshot GetProductionJob(int slotIndex)
        {
            var slot = GetFactorySlot(slotIndex);
            if (slot == null || slot.activeJobRecipe == SemiconRecipeKind.None ||
                slot.activeJobFinishUtcTicks <= slot.activeJobStartUtcTicks)
            {
                return new SemiconProductionJobSnapshot(false, false, SemiconRecipeKind.None, 0, 0, 0,
                    0f, 0f, 0f);
            }

            var nowTicks = DateTime.UtcNow.Ticks;
            var totalTicks = Math.Max(1L, slot.activeJobFinishUtcTicks - slot.activeJobStartUtcTicks);
            var remainingTicks = Math.Max(0L, slot.activeJobFinishUtcTicks - nowTicks);
            var elapsedTicks = Math.Max(0L, Math.Min(totalTicks, nowTicks - slot.activeJobStartUtcTicks));
            var progress = Mathf.Clamp01((float)((double)elapsedTicks / totalTicks));
            var remainingSeconds = (float)((double)remainingTicks / TimeSpan.TicksPerSecond);
            var totalSeconds = (float)((double)totalTicks / TimeSpan.TicksPerSecond);
            return new SemiconProductionJobSnapshot(true, remainingTicks <= 0L, slot.activeJobRecipe,
                slot.activeJobBatches, slot.activeJobOutput, slot.activeJobQuality, progress,
                remainingSeconds, totalSeconds);
        }

        public bool TryStartProduction(int slotIndex, SemiconRecipeKind recipe, int batches,
            out SemiconProductionJobSnapshot job, out string reason)
        {
            job = default;
            if (batches <= 0 || recipe == SemiconRecipeKind.None)
            {
                reason = "생산할 레시피와 수량을 선택하세요.";
                return false;
            }

            var slot = GetFactorySlot(slotIndex);
            if (slot == null || !slot.machineInstalled)
            {
                reason = "선택한 슬롯에 생산 설비가 없습니다.";
                return false;
            }
            if (GetProductionJob(slotIndex).HasJob)
            {
                reason = "설비가 이미 다른 생산 작업을 진행 중입니다.";
                return false;
            }
            var isPackagedProduct = recipe == SemiconRecipeKind.Sc01ControlSensor ||
                                    recipe == SemiconRecipeKind.Pm10PowerManagement ||
                                    recipe == SemiconRecipeKind.Dd20DisplayDriver;
            if (isPackagedProduct && !packageRecipeQualified)
            {
                reason = "패키징 공정 실험에서 PACKAGE-01 레시피를 먼저 확정하세요.";
                return false;
            }
            if (recipe == SemiconRecipeKind.Pm10PowerManagement &&
                !IsContractUnlocked(SemiconContractKind.Pm10PowerManagement))
            {
                reason = "공정 샘플 계약 2종을 완료하면 PM-10 제품이 개방됩니다.";
                return false;
            }
            if (recipe == SemiconRecipeKind.Dd20DisplayDriver &&
                !IsContractUnlocked(SemiconContractKind.Dd20DisplayDriver))
            {
                reason = "샘플 계약 4종과 PM-10 계약을 완료하면 DD-20 제품이 개방됩니다.";
                return false;
            }

            if (recipe == SemiconRecipeKind.OxidizedWafer && !oxidationRecipeQualified)
            {
                reason = "산화 공정 실험에서 OXIDE-01 레시피를 먼저 확정하세요.";
                return false;
            }
            if (recipe == SemiconRecipeKind.PhotoPatternedWafer && !photoRecipeQualified)
            {
                reason = "포토 공정 실험에서 PHOTO-01 레시피를 먼저 확정하세요.";
                return false;
            }
            if (recipe == SemiconRecipeKind.EtchedWafer && !etchRecipeQualified)
            {
                reason = "식각 공정 실험에서 ETCH-01 레시피를 먼저 확정하세요.";
                return false;
            }
            if (recipe == SemiconRecipeKind.DepositedWafer && !depositionRecipeQualified)
            {
                reason = "증착 공정 실험에서 DEPO-01 레시피를 먼저 확정하세요.";
                return false;
            }
            if (recipe == SemiconRecipeKind.MetalizedWafer && !metalRecipeQualified)
            {
                reason = "금속 배선 공정 실험에서 METAL-01 레시피를 먼저 확정하세요.";
                return false;
            }
            if (recipe == SemiconRecipeKind.TestedWafer && !edsRecipeQualified)
            {
                reason = "EDS 공정 실험에서 EDS-01 레시피를 먼저 확정하세요.";
                return false;
            }

            var siliconCost = recipe switch
            {
                SemiconRecipeKind.WaferSubstrate => WaferSiliconCost * batches,
                _ => 0
            };
            var gasCost = recipe switch
            {
                SemiconRecipeKind.OxidizedWafer => OxidationGasCost * batches,
                SemiconRecipeKind.EtchedWafer => EtchGasCost * batches,
                SemiconRecipeKind.DepositedWafer => DepositionGasCost * batches,
                _ => 0
            };
            var chemicalCost = recipe switch
            {
                SemiconRecipeKind.PhotoPatternedWafer => PhotoChemicalCost * batches,
                SemiconRecipeKind.Sc01ControlSensor => PackageChemicalCost * batches,
                SemiconRecipeKind.Pm10PowerManagement => PackageChemicalCost * batches,
                SemiconRecipeKind.Dd20DisplayDriver => PackageChemicalCost * batches,
                _ => 0
            };
            var metalTargetCost = recipe == SemiconRecipeKind.MetalizedWafer
                ? MetalTargetCost * batches
                : 0;
            var waferCost = recipe == SemiconRecipeKind.OxidizedWafer ? OxidationWaferCost * batches : 0;
            var oxidizedWaferCost = recipe == SemiconRecipeKind.PhotoPatternedWafer
                ? PhotoOxidizedWaferCost * batches
                : 0;
            var patternedWaferCost = recipe == SemiconRecipeKind.EtchedWafer
                ? EtchPatternedWaferCost * batches
                : 0;
            var etchedWaferCost = recipe == SemiconRecipeKind.DepositedWafer
                ? DepositionEtchedWaferCost * batches
                : 0;
            var depositedWaferCost = recipe == SemiconRecipeKind.MetalizedWafer
                ? MetalDepositedWaferCost * batches
                : 0;
            var metalizedWaferCost = recipe == SemiconRecipeKind.TestedWafer
                ? EdsMetalizedWaferCost * batches
                : 0;
            var testedWaferCost = isPackagedProduct
                ? PackageTestedWaferCost * batches
                : 0;
            if (siliconStock < siliconCost || processGasStock < gasCost || chemicalStock < chemicalCost ||
                metalTargetStock < metalTargetCost ||
                waferStock < waferCost || oxidizedWaferStock < oxidizedWaferCost ||
                patternedWaferStock < patternedWaferCost || etchedWaferStock < etchedWaferCost ||
                depositedWaferStock < depositedWaferCost || metalizedWaferStock < metalizedWaferCost ||
                testedWaferStock < testedWaferCost)
            {
                reason = "선택한 공정에 필요한 원재료 재고가 부족합니다.";
                return false;
            }

            var stats = GetProductionStats(slotIndex);
            var baseSeconds = SemiconFactoryDefinitions.GetBaseCycleSeconds(recipe);
            var durationSeconds = baseSeconds * batches * 100f / Math.Max(1, stats.Speed);
            var nowTicks = DateTime.UtcNow.Ticks;
            siliconStock -= siliconCost;
            processGasStock -= gasCost;
            chemicalStock -= chemicalCost;
            metalTargetStock -= metalTargetCost;
            var inputWaferQuality = AverageWaferQuality;
            var inputOxidizedWaferQuality = AverageOxidizedWaferQuality;
            var inputPatternedWaferQuality = AveragePatternedWaferQuality;
            var inputEtchedWaferQuality = AverageEtchedWaferQuality;
            var inputDepositedWaferQuality = AverageDepositedWaferQuality;
            var inputMetalizedWaferQuality = AverageMetalizedWaferQuality;
            var inputTestedWaferQuality = AverageTestedWaferQuality;
            if (waferCost > 0)
            {
                waferStock -= waferCost;
                waferQualityPoints = Math.Max(0, waferQualityPoints - inputWaferQuality * waferCost);
            }
            if (oxidizedWaferCost > 0)
            {
                oxidizedWaferStock -= oxidizedWaferCost;
                oxidizedWaferQualityPoints = Math.Max(0,
                    oxidizedWaferQualityPoints - inputOxidizedWaferQuality * oxidizedWaferCost);
            }
            if (patternedWaferCost > 0)
            {
                patternedWaferStock -= patternedWaferCost;
                patternedWaferQualityPoints = Math.Max(0,
                    patternedWaferQualityPoints - inputPatternedWaferQuality * patternedWaferCost);
            }
            if (etchedWaferCost > 0)
            {
                etchedWaferStock -= etchedWaferCost;
                etchedWaferQualityPoints = Math.Max(0,
                    etchedWaferQualityPoints - inputEtchedWaferQuality * etchedWaferCost);
            }
            if (depositedWaferCost > 0)
            {
                depositedWaferStock -= depositedWaferCost;
                depositedWaferQualityPoints = Math.Max(0,
                    depositedWaferQualityPoints - inputDepositedWaferQuality * depositedWaferCost);
            }
            if (metalizedWaferCost > 0)
            {
                metalizedWaferStock -= metalizedWaferCost;
                metalizedWaferQualityPoints = Math.Max(0,
                    metalizedWaferQualityPoints - inputMetalizedWaferQuality * metalizedWaferCost);
            }
            if (testedWaferCost > 0)
            {
                testedWaferStock -= testedWaferCost;
                testedWaferQualityPoints = Math.Max(0,
                    testedWaferQualityPoints - inputTestedWaferQuality * testedWaferCost);
            }
            slot.activeJobRecipe = recipe;
            slot.activeJobBatches = batches;
            slot.activeJobOutput = stats.OutputPerCycle * batches;
            slot.activeJobQuality = recipe == SemiconRecipeKind.OxidizedWafer
                ? Mathf.Clamp(Mathf.RoundToInt(inputWaferQuality * 0.35f + stats.Quality * 0.30f +
                                               bestOxideUniformity * 0.35f), 1, 100)
                : recipe == SemiconRecipeKind.PhotoPatternedWafer
                    ? Mathf.Clamp(Mathf.RoundToInt(inputOxidizedWaferQuality * 0.40f + stats.Quality * 0.30f +
                                                   (bestYield + bestPrecision) * 0.15f), 1, 100)
                    : recipe == SemiconRecipeKind.EtchedWafer
                        ? Mathf.Clamp(Mathf.RoundToInt(inputPatternedWaferQuality * 0.40f +
                                                       stats.Quality * 0.30f + bestEtchProfile * 0.30f), 1, 100)
                        : recipe == SemiconRecipeKind.DepositedWafer
                            ? Mathf.Clamp(Mathf.RoundToInt(inputEtchedWaferQuality * 0.40f +
                                                           stats.Quality * 0.30f +
                                                           bestDepositionUniformity * 0.18f +
                                                           bestDepositionCoverage * 0.12f), 1, 100)
                            : recipe == SemiconRecipeKind.MetalizedWafer
                                ? Mathf.Clamp(Mathf.RoundToInt(inputDepositedWaferQuality * 0.40f +
                                                               stats.Quality * 0.30f +
                                                               bestMetalAdhesion * 0.20f +
                                                               Mathf.Clamp(100f - Mathf.Abs(bestMetalResistance - 0.1f) *
                                                                   250f, 60f, 100f) * 0.10f), 1, 100)
                                : recipe == SemiconRecipeKind.TestedWafer
                                    ? Mathf.Clamp(Mathf.RoundToInt(inputMetalizedWaferQuality * 0.40f +
                                                                   stats.Quality * 0.30f +
                                                                   bestEdsDetection * 0.20f +
                                                                   bestEdsYield * 0.10f), 1, 100)
                                    : isPackagedProduct
                                        ? Mathf.Clamp(Mathf.RoundToInt(inputTestedWaferQuality * 0.40f +
                                                                       stats.Quality * 0.25f +
                                                                       bestPackageBondStrength * 0.15f +
                                                                       bestPackageIntegrity * 0.10f +
                                                                       bestPackageFinalPass * 0.10f), 1, 100)
                                        : stats.Quality;
            slot.activeJobStartUtcTicks = nowTicks;
            slot.activeJobFinishUtcTicks = nowTicks + TimeSpan.FromSeconds(durationSeconds).Ticks;
            SaveAndNotify();
            job = GetProductionJob(slotIndex);
            reason = string.Empty;
            return true;
        }

        public bool TryCollectProduction(int slotIndex, out SemiconRecipeKind recipe, out int outputAmount,
            out int quality, out string reason)
        {
            recipe = SemiconRecipeKind.None;
            outputAmount = 0;
            quality = 0;
            var slot = GetFactorySlot(slotIndex);
            var job = GetProductionJob(slotIndex);
            if (slot == null || !job.HasJob)
            {
                reason = "회수할 생산 작업이 없습니다.";
                return false;
            }
            if (!job.IsComplete)
            {
                reason = $"생산 완료까지 {job.RemainingSeconds:0.0}초 남았습니다.";
                return false;
            }

            recipe = job.Recipe;
            outputAmount = job.OutputAmount;
            quality = job.Quality;
            if (recipe == SemiconRecipeKind.WaferSubstrate)
            {
                waferStock += outputAmount;
                waferQualityPoints += quality * outputAmount;
            }
            else if (recipe == SemiconRecipeKind.Sc01ControlSensor)
            {
                finishedProductStock += outputAmount;
                finishedProductQualityPoints += quality * outputAmount;
            }
            else if (recipe == SemiconRecipeKind.Pm10PowerManagement)
            {
                pm10Stock += outputAmount;
                pm10QualityPoints += quality * outputAmount;
            }
            else if (recipe == SemiconRecipeKind.Dd20DisplayDriver)
            {
                dd20Stock += outputAmount;
                dd20QualityPoints += quality * outputAmount;
            }
            else if (recipe == SemiconRecipeKind.OxidizedWafer)
            {
                oxidizedWaferStock += outputAmount;
                oxidizedWaferQualityPoints += quality * outputAmount;
            }
            else if (recipe == SemiconRecipeKind.PhotoPatternedWafer)
            {
                patternedWaferStock += outputAmount;
                patternedWaferQualityPoints += quality * outputAmount;
            }
            else if (recipe == SemiconRecipeKind.EtchedWafer)
            {
                etchedWaferStock += outputAmount;
                etchedWaferQualityPoints += quality * outputAmount;
            }
            else if (recipe == SemiconRecipeKind.DepositedWafer)
            {
                depositedWaferStock += outputAmount;
                depositedWaferQualityPoints += quality * outputAmount;
            }
            else if (recipe == SemiconRecipeKind.MetalizedWafer)
            {
                metalizedWaferStock += outputAmount;
                metalizedWaferQualityPoints += quality * outputAmount;
            }
            else if (recipe == SemiconRecipeKind.TestedWafer)
            {
                testedWaferStock += outputAmount;
                testedWaferQualityPoints += quality * outputAmount;
            }

            unlockedProcessCount = Math.Max(unlockedProcessCount, recipe switch
            {
                SemiconRecipeKind.WaferSubstrate => 2,
                SemiconRecipeKind.OxidizedWafer => 3,
                SemiconRecipeKind.PhotoPatternedWafer => 4,
                SemiconRecipeKind.EtchedWafer => 5,
                SemiconRecipeKind.DepositedWafer => 6,
                SemiconRecipeKind.MetalizedWafer => 7,
                SemiconRecipeKind.TestedWafer => 8,
                _ => unlockedProcessCount
            });

            slot.ClearJob();
            EnsureProgressArrays();
            if ((int)recipe >= 0 && (int)recipe < lifetimeProduced.Length)
            {
                lifetimeProduced[(int)recipe] += outputAmount;
                bestProducedQuality[(int)recipe] = Math.Max(bestProducedQuality[(int)recipe], quality);
            }
            SaveAndNotify();
            reason = string.Empty;
            return true;
        }

        public int GetRecipeOutputStock(SemiconRecipeKind recipe)
        {
            return recipe switch
            {
                SemiconRecipeKind.WaferSubstrate => waferStock,
                SemiconRecipeKind.OxidizedWafer => oxidizedWaferStock,
                SemiconRecipeKind.PhotoPatternedWafer => patternedWaferStock,
                SemiconRecipeKind.EtchedWafer => etchedWaferStock,
                SemiconRecipeKind.DepositedWafer => depositedWaferStock,
                SemiconRecipeKind.MetalizedWafer => metalizedWaferStock,
                SemiconRecipeKind.TestedWafer => testedWaferStock,
                SemiconRecipeKind.Sc01ControlSensor => finishedProductStock,
                SemiconRecipeKind.Pm10PowerManagement => pm10Stock,
                SemiconRecipeKind.Dd20DisplayDriver => dd20Stock,
                _ => 0
            };
        }

        public SemiconFactorySlotData GetFactorySlot(int slotIndex)
        {
            EnsureFactorySlots();
            return slotIndex >= 0 && slotIndex < factorySlots.Length ? factorySlots[slotIndex] : null;
        }

        public SemiconProductionStats GetProductionStats(int slotIndex)
        {
            var slot = GetFactorySlot(slotIndex);
            return slot == null
                ? SemiconFactoryDefinitions.GetStats(SemiconWorkerKind.None, SemiconDiskKind.None)
                : SemiconFactoryDefinitions.GetStats(slot.worker, slot.disk);
        }

        public bool IsWorkerAvailable(SemiconWorkerKind worker, int targetSlot)
        {
            if (worker == SemiconWorkerKind.None) return true;
            EnsureFactorySlots();
            for (var index = 0; index < factorySlots.Length; index++)
            {
                if (index != targetSlot && factorySlots[index].worker == worker) return false;
            }
            return true;
        }

        public bool IsDiskAvailable(SemiconDiskKind disk, int targetSlot)
        {
            if (disk == SemiconDiskKind.None) return true;
            EnsureFactorySlots();
            for (var index = 0; index < factorySlots.Length; index++)
            {
                if (index != targetSlot && factorySlots[index].disk == disk) return false;
            }
            return true;
        }

        public bool TryInstallFactoryMachine(int slotIndex, out string reason)
        {
            var slot = GetFactorySlot(slotIndex);
            if (slot == null)
            {
                reason = "존재하지 않는 설비 슬롯입니다.";
                return false;
            }
            if (slot.machineInstalled)
            {
                reason = "이미 SC-01 조립 설비가 배치되어 있습니다.";
                return false;
            }
            if (credits < SemiconFactoryDefinitions.MachineInstallPrice)
            {
                reason = $"설비 배치에 ₩ {SemiconFactoryDefinitions.MachineInstallPrice:N0}이 필요합니다.";
                return false;
            }

            credits -= SemiconFactoryDefinitions.MachineInstallPrice;
            slot.machineInstalled = true;
            SaveAndNotify();
            reason = string.Empty;
            return true;
        }

        public bool TryAssignWorker(int slotIndex, SemiconWorkerKind worker, out string reason)
        {
            var slot = GetFactorySlot(slotIndex);
            if (slot == null || !slot.machineInstalled)
            {
                reason = "먼저 생산 설비를 배치하세요.";
                return false;
            }
            if (GetProductionJob(slotIndex).HasJob)
            {
                reason = "생산 중에는 작업 인력을 변경할 수 없습니다.";
                return false;
            }
            if (!IsWorkerAvailable(worker, slotIndex))
            {
                reason = "해당 인력은 다른 설비에 배정되어 있습니다.";
                return false;
            }

            slot.worker = worker;
            SaveAndNotify();
            reason = string.Empty;
            return true;
        }

        public bool TryAssignDisk(int slotIndex, SemiconDiskKind disk, out string reason)
        {
            var slot = GetFactorySlot(slotIndex);
            if (slot == null || !slot.machineInstalled)
            {
                reason = "먼저 생산 설비를 배치하세요.";
                return false;
            }
            if (GetProductionJob(slotIndex).HasJob)
            {
                reason = "생산 중에는 디스크를 교체할 수 없습니다.";
                return false;
            }
            if (!IsDiskAvailable(disk, slotIndex))
            {
                reason = "해당 디스크는 다른 설비에 장착되어 있습니다.";
                return false;
            }

            slot.disk = disk;
            SaveAndNotify();
            reason = string.Empty;
            return true;
        }

        public int GetFinishedProductSalePrice()
        {
            return FinishedProductSalePrice + Math.Max(0, AverageFinishedProductQuality - 80) * 40;
        }

        [ContextMenu("Reset Prototype Save")]
        public void ResetProgress()
        {
            researchPoints = 120;
            credits = 25000;
            bestYield = 0f;
            bestPrecision = 0f;
            bestDose = 90;
            bestFocus = -0.15f;
            experimentCount = 0;
            photoRecipeQualified = false;
            oxidationExperimentCount = 0;
            oxidationRecipeQualified = false;
            bestOxidationTemperature = 1000;
            bestOxidationTime = 60;
            bestOxideThickness = 0f;
            bestOxideUniformity = 0f;
            etchExperimentCount = 0;
            etchRecipeQualified = false;
            bestEtchPower = 250;
            bestEtchGasFlow = 60;
            bestEtchDepth = 0f;
            bestEtchProfile = 0f;
            depositionExperimentCount = 0;
            depositionRecipeQualified = false;
            bestDepositionTemperature = 400;
            bestDepositionPressure = 6;
            bestDepositionThickness = 0f;
            bestDepositionUniformity = 0f;
            bestDepositionCoverage = 0f;
            metalExperimentCount = 0;
            metalRecipeQualified = false;
            bestMetalPower = 250;
            bestMetalTime = 60;
            bestMetalThickness = 0f;
            bestMetalResistance = 0f;
            bestMetalAdhesion = 0f;
            edsExperimentCount = 0;
            edsRecipeQualified = false;
            bestEdsVoltage = 3;
            bestEdsLeakageThreshold = 30;
            bestEdsYield = 0f;
            bestEdsDetection = 0f;
            bestEdsFalseReject = 0f;
            packageExperimentCount = 0;
            packageRecipeQualified = false;
            bestPackageBondingForce = 35;
            bestPackageMoldingTemperature = 175;
            bestPackageBondStrength = 0f;
            bestPackageIntegrity = 0f;
            bestPackageFinalPass = 0f;
            firstTutorialCompleted = false;
            unlockedProcessCount = 1;
            firstOrderAccepted = false;
            firstOrderCompleted = false;
            siliconStock = 0;
            processGasStock = 0;
            chemicalStock = 0;
            metalTargetStock = 0;
            finishedProductStock = 0;
            finishedProductQualityPoints = 0;
            pm10Stock = 0;
            pm10QualityPoints = 0;
            dd20Stock = 0;
            dd20QualityPoints = 0;
            activeContractId = -1;
            contractCompletionCounts = new int[SemiconContractCatalog.Count];
            lifetimeProduced = new int[Enum.GetValues(typeof(SemiconRecipeKind)).Length];
            bestProducedQuality = new int[Enum.GetValues(typeof(SemiconRecipeKind)).Length];
            waferStock = 0;
            waferQualityPoints = 0;
            oxidizedWaferStock = 0;
            oxidizedWaferQualityPoints = 0;
            patternedWaferStock = 0;
            patternedWaferQualityPoints = 0;
            etchedWaferStock = 0;
            etchedWaferQualityPoints = 0;
            depositedWaferStock = 0;
            depositedWaferQualityPoints = 0;
            metalizedWaferStock = 0;
            metalizedWaferQualityPoints = 0;
            testedWaferStock = 0;
            testedWaferQualityPoints = 0;
            factorySlots = CreateDefaultFactorySlots();
            SaveAndNotify();
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if (data == null) return;

                researchPoints = data.researchPoints;
                credits = data.credits;
                bestYield = data.bestYield;
                bestPrecision = data.bestPrecision;
                bestDose = data.bestDose;
                bestFocus = data.bestFocus;
                experimentCount = data.experimentCount;
                photoRecipeQualified = data.photoRecipeQualified;
                oxidationExperimentCount = data.oxidationExperimentCount;
                oxidationRecipeQualified = data.oxidationRecipeQualified;
                bestOxidationTemperature = data.bestOxidationTemperature == 0 ? 1000 : data.bestOxidationTemperature;
                bestOxidationTime = data.bestOxidationTime == 0 ? 60 : data.bestOxidationTime;
                bestOxideThickness = data.bestOxideThickness;
                bestOxideUniformity = data.bestOxideUniformity;
                etchExperimentCount = data.etchExperimentCount;
                etchRecipeQualified = data.etchRecipeQualified;
                bestEtchPower = data.bestEtchPower == 0 ? 250 : data.bestEtchPower;
                bestEtchGasFlow = data.bestEtchGasFlow == 0 ? 60 : data.bestEtchGasFlow;
                bestEtchDepth = data.bestEtchDepth;
                bestEtchProfile = data.bestEtchProfile;
                depositionExperimentCount = data.depositionExperimentCount;
                depositionRecipeQualified = data.depositionRecipeQualified;
                bestDepositionTemperature = data.bestDepositionTemperature == 0 ? 400 : data.bestDepositionTemperature;
                bestDepositionPressure = data.bestDepositionPressure == 0 ? 6 : data.bestDepositionPressure;
                bestDepositionThickness = data.bestDepositionThickness;
                bestDepositionUniformity = data.bestDepositionUniformity;
                bestDepositionCoverage = data.bestDepositionCoverage;
                metalExperimentCount = data.metalExperimentCount;
                metalRecipeQualified = data.metalRecipeQualified;
                bestMetalPower = data.bestMetalPower == 0 ? 250 : data.bestMetalPower;
                bestMetalTime = data.bestMetalTime == 0 ? 60 : data.bestMetalTime;
                bestMetalThickness = data.bestMetalThickness;
                bestMetalResistance = data.bestMetalResistance;
                bestMetalAdhesion = data.bestMetalAdhesion;
                edsExperimentCount = data.edsExperimentCount;
                edsRecipeQualified = data.edsRecipeQualified;
                bestEdsVoltage = data.bestEdsVoltage == 0 ? 3 : data.bestEdsVoltage;
                bestEdsLeakageThreshold = data.bestEdsLeakageThreshold == 0 ? 30 : data.bestEdsLeakageThreshold;
                bestEdsYield = data.bestEdsYield;
                bestEdsDetection = data.bestEdsDetection;
                bestEdsFalseReject = data.bestEdsFalseReject;
                packageExperimentCount = data.packageExperimentCount;
                packageRecipeQualified = data.packageRecipeQualified;
                bestPackageBondingForce = data.bestPackageBondingForce == 0 ? 35 : data.bestPackageBondingForce;
                bestPackageMoldingTemperature = data.bestPackageMoldingTemperature == 0
                    ? 175
                    : data.bestPackageMoldingTemperature;
                bestPackageBondStrength = data.bestPackageBondStrength;
                bestPackageIntegrity = data.bestPackageIntegrity;
                bestPackageFinalPass = data.bestPackageFinalPass;
                firstTutorialCompleted = data.firstTutorialCompleted;
                unlockedProcessCount = Math.Max(data.unlockedProcessCount, InferUnlockedProcessCount(data));
                firstOrderAccepted = data.firstOrderAccepted;
                firstOrderCompleted = data.firstOrderCompleted;
                siliconStock = data.siliconStock;
                processGasStock = data.processGasStock;
                chemicalStock = data.chemicalStock;
                metalTargetStock = data.metalTargetStock;
                finishedProductStock = data.finishedProductStock;
                finishedProductQualityPoints = data.finishedProductQualityPoints;
                pm10Stock = data.pm10Stock;
                pm10QualityPoints = data.pm10QualityPoints;
                dd20Stock = data.dd20Stock;
                dd20QualityPoints = data.dd20QualityPoints;
                activeContractId = data.contractCompletionCounts == null ? -1 : data.activeContractId;
                contractCompletionCounts = data.contractCompletionCounts;
                lifetimeProduced = data.lifetimeProduced;
                bestProducedQuality = data.bestProducedQuality;
                waferStock = data.waferStock;
                waferQualityPoints = data.waferQualityPoints;
                oxidizedWaferStock = data.oxidizedWaferStock;
                oxidizedWaferQualityPoints = data.oxidizedWaferQualityPoints;
                patternedWaferStock = data.patternedWaferStock;
                patternedWaferQualityPoints = data.patternedWaferQualityPoints;
                etchedWaferStock = data.etchedWaferStock;
                etchedWaferQualityPoints = data.etchedWaferQualityPoints;
                depositedWaferStock = data.depositedWaferStock;
                depositedWaferQualityPoints = data.depositedWaferQualityPoints;
                metalizedWaferStock = data.metalizedWaferStock;
                metalizedWaferQualityPoints = data.metalizedWaferQualityPoints;
                testedWaferStock = data.testedWaferStock;
                testedWaferQualityPoints = data.testedWaferQualityPoints;
                factorySlots = data.factorySlots;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"세이브 데이터를 불러오지 못했습니다: {exception.Message}");
            }
        }

        private void SaveAndNotify()
        {
            Save();
            StateChanged?.Invoke();
        }

        private void Save()
        {
            if (suppressPersistence)
            {
                return;
            }
            try
            {
                var data = new SaveData
                {
                    researchPoints = researchPoints,
                    credits = credits,
                    bestYield = bestYield,
                    bestPrecision = bestPrecision,
                    bestDose = bestDose,
                    bestFocus = bestFocus,
                    experimentCount = experimentCount,
                    photoRecipeQualified = photoRecipeQualified,
                    oxidationExperimentCount = oxidationExperimentCount,
                    oxidationRecipeQualified = oxidationRecipeQualified,
                    bestOxidationTemperature = bestOxidationTemperature,
                    bestOxidationTime = bestOxidationTime,
                    bestOxideThickness = bestOxideThickness,
                    bestOxideUniformity = bestOxideUniformity,
                    etchExperimentCount = etchExperimentCount,
                    etchRecipeQualified = etchRecipeQualified,
                    bestEtchPower = bestEtchPower,
                    bestEtchGasFlow = bestEtchGasFlow,
                    bestEtchDepth = bestEtchDepth,
                    bestEtchProfile = bestEtchProfile,
                    depositionExperimentCount = depositionExperimentCount,
                    depositionRecipeQualified = depositionRecipeQualified,
                    bestDepositionTemperature = bestDepositionTemperature,
                    bestDepositionPressure = bestDepositionPressure,
                    bestDepositionThickness = bestDepositionThickness,
                    bestDepositionUniformity = bestDepositionUniformity,
                    bestDepositionCoverage = bestDepositionCoverage,
                    metalExperimentCount = metalExperimentCount,
                    metalRecipeQualified = metalRecipeQualified,
                    bestMetalPower = bestMetalPower,
                    bestMetalTime = bestMetalTime,
                    bestMetalThickness = bestMetalThickness,
                    bestMetalResistance = bestMetalResistance,
                    bestMetalAdhesion = bestMetalAdhesion,
                    edsExperimentCount = edsExperimentCount,
                    edsRecipeQualified = edsRecipeQualified,
                    bestEdsVoltage = bestEdsVoltage,
                    bestEdsLeakageThreshold = bestEdsLeakageThreshold,
                    bestEdsYield = bestEdsYield,
                    bestEdsDetection = bestEdsDetection,
                    bestEdsFalseReject = bestEdsFalseReject,
                    packageExperimentCount = packageExperimentCount,
                    packageRecipeQualified = packageRecipeQualified,
                    bestPackageBondingForce = bestPackageBondingForce,
                    bestPackageMoldingTemperature = bestPackageMoldingTemperature,
                    bestPackageBondStrength = bestPackageBondStrength,
                    bestPackageIntegrity = bestPackageIntegrity,
                    bestPackageFinalPass = bestPackageFinalPass,
                    firstTutorialCompleted = firstTutorialCompleted,
                    unlockedProcessCount = unlockedProcessCount,
                    firstOrderAccepted = firstOrderAccepted,
                    firstOrderCompleted = firstOrderCompleted,
                    siliconStock = siliconStock,
                    processGasStock = processGasStock,
                    chemicalStock = chemicalStock,
                    metalTargetStock = metalTargetStock,
                    finishedProductStock = finishedProductStock,
                    finishedProductQualityPoints = finishedProductQualityPoints,
                    pm10Stock = pm10Stock,
                    pm10QualityPoints = pm10QualityPoints,
                    dd20Stock = dd20Stock,
                    dd20QualityPoints = dd20QualityPoints,
                    activeContractId = activeContractId,
                    contractCompletionCounts = contractCompletionCounts,
                    lifetimeProduced = lifetimeProduced,
                    bestProducedQuality = bestProducedQuality,
                    waferStock = waferStock,
                    waferQualityPoints = waferQualityPoints,
                    oxidizedWaferStock = oxidizedWaferStock,
                    oxidizedWaferQualityPoints = oxidizedWaferQualityPoints,
                    patternedWaferStock = patternedWaferStock,
                    patternedWaferQualityPoints = patternedWaferQualityPoints,
                    etchedWaferStock = etchedWaferStock,
                    etchedWaferQualityPoints = etchedWaferQualityPoints,
                    depositedWaferStock = depositedWaferStock,
                    depositedWaferQualityPoints = depositedWaferQualityPoints,
                    metalizedWaferStock = metalizedWaferStock,
                    metalizedWaferQualityPoints = metalizedWaferQualityPoints,
                    testedWaferStock = testedWaferStock,
                    testedWaferQualityPoints = testedWaferQualityPoints,
                    factorySlots = factorySlots
                };
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"세이브 데이터를 저장하지 못했습니다: {exception.Message}");
            }
        }

        private void EnsureFactorySlots()
        {
            if (factorySlots != null && factorySlots.Length == SemiconFactoryDefinitions.SlotCount)
            {
                for (var index = 0; index < factorySlots.Length; index++)
                {
                    if (factorySlots[index] == null)
                    {
                        factorySlots[index] = new SemiconFactorySlotData(index == 0);
                    }
                }
                return;
            }

            var previous = factorySlots;
            factorySlots = CreateDefaultFactorySlots();
            if (previous == null) return;
            for (var index = 0; index < Math.Min(previous.Length, factorySlots.Length); index++)
            {
                if (previous[index] != null) factorySlots[index] = previous[index];
            }
        }

        private void EnsureProgressArrays()
        {
            if (contractCompletionCounts == null || contractCompletionCounts.Length != SemiconContractCatalog.Count)
            {
                var previous = contractCompletionCounts;
                contractCompletionCounts = new int[SemiconContractCatalog.Count];
                if (previous != null) Array.Copy(previous, contractCompletionCounts,
                    Math.Min(previous.Length, contractCompletionCounts.Length));
            }

            var recipeCount = Enum.GetValues(typeof(SemiconRecipeKind)).Length;
            if (lifetimeProduced == null || lifetimeProduced.Length != recipeCount)
            {
                var previous = lifetimeProduced;
                lifetimeProduced = new int[recipeCount];
                if (previous != null) Array.Copy(previous, lifetimeProduced,
                    Math.Min(previous.Length, lifetimeProduced.Length));
            }
            if (bestProducedQuality == null || bestProducedQuality.Length != recipeCount)
            {
                var previous = bestProducedQuality;
                bestProducedQuality = new int[recipeCount];
                if (previous != null) Array.Copy(previous, bestProducedQuality,
                    Math.Min(previous.Length, bestProducedQuality.Length));
            }
        }

        private void RemoveRecipeStock(SemiconRecipeKind recipe, int amount, int quality)
        {
            switch (recipe)
            {
                case SemiconRecipeKind.OxidizedWafer:
                    oxidizedWaferStock -= amount; oxidizedWaferQualityPoints = Math.Max(0, oxidizedWaferQualityPoints - quality * amount); break;
                case SemiconRecipeKind.PhotoPatternedWafer:
                    patternedWaferStock -= amount; patternedWaferQualityPoints = Math.Max(0, patternedWaferQualityPoints - quality * amount); break;
                case SemiconRecipeKind.EtchedWafer:
                    etchedWaferStock -= amount; etchedWaferQualityPoints = Math.Max(0, etchedWaferQualityPoints - quality * amount); break;
                case SemiconRecipeKind.DepositedWafer:
                    depositedWaferStock -= amount; depositedWaferQualityPoints = Math.Max(0, depositedWaferQualityPoints - quality * amount); break;
                case SemiconRecipeKind.MetalizedWafer:
                    metalizedWaferStock -= amount; metalizedWaferQualityPoints = Math.Max(0, metalizedWaferQualityPoints - quality * amount); break;
                case SemiconRecipeKind.TestedWafer:
                    testedWaferStock -= amount; testedWaferQualityPoints = Math.Max(0, testedWaferQualityPoints - quality * amount); break;
                case SemiconRecipeKind.Sc01ControlSensor:
                    finishedProductStock -= amount; finishedProductQualityPoints = Math.Max(0, finishedProductQualityPoints - quality * amount); break;
                case SemiconRecipeKind.Pm10PowerManagement:
                    pm10Stock -= amount; pm10QualityPoints = Math.Max(0, pm10QualityPoints - quality * amount); break;
                case SemiconRecipeKind.Dd20DisplayDriver:
                    dd20Stock -= amount; dd20QualityPoints = Math.Max(0, dd20QualityPoints - quality * amount); break;
            }
        }

        private static int InferUnlockedProcessCount(SaveData data)
        {
            if (data.testedWaferStock > 0 || data.packageRecipeQualified || data.packageExperimentCount > 0 ||
                data.finishedProductStock > 0 || data.firstOrderAccepted || data.firstOrderCompleted) return 8;
            if (data.metalizedWaferStock > 0 || data.edsRecipeQualified || data.edsExperimentCount > 0) return 7;
            if (data.depositedWaferStock > 0 || data.metalRecipeQualified || data.metalExperimentCount > 0) return 6;
            if (data.etchedWaferStock > 0 || data.depositionRecipeQualified || data.depositionExperimentCount > 0) return 5;
            if (data.patternedWaferStock > 0 || data.etchRecipeQualified || data.etchExperimentCount > 0) return 4;
            if (data.oxidizedWaferStock > 0 || data.photoRecipeQualified || data.experimentCount > 0) return 3;
            if (data.waferStock > 0 || data.oxidationRecipeQualified || data.oxidationExperimentCount > 0 ||
                data.firstTutorialCompleted) return 2;
            return 1;
        }

        private static SemiconFactorySlotData[] CreateDefaultFactorySlots()
        {
            return new[]
            {
                new SemiconFactorySlotData(true),
                new SemiconFactorySlotData(false),
                new SemiconFactorySlotData(false)
            };
        }
    }
}
