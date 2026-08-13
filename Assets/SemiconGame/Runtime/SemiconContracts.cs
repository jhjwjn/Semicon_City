using System;

namespace SemiconCity.Game
{
    public enum SemiconContractKind
    {
        None = -1,
        OxideEvaluation = 0,
        PhotoMaskVerification = 1,
        EtchProfileSample = 2,
        ThinFilmUniformity = 3,
        MetalResistanceTest = 4,
        KnownGoodDie = 5,
        Sc01IndustrialSensor = 6,
        Pm10PowerManagement = 7,
        Dd20DisplayDriver = 8
    }

    public readonly struct SemiconContractDefinition
    {
        public readonly SemiconContractKind Kind;
        public readonly string Code;
        public readonly string Name;
        public readonly string Client;
        public readonly SemiconRecipeKind RequiredRecipe;
        public readonly int RequiredAmount;
        public readonly int MinimumQuality;
        public readonly int CreditReward;
        public readonly int ResearchReward;
        public readonly string Description;

        public SemiconContractDefinition(SemiconContractKind kind, string code, string name, string client,
            SemiconRecipeKind recipe, int amount, int quality, int credits, int research, string description)
        {
            Kind = kind;
            Code = code;
            Name = name;
            Client = client;
            RequiredRecipe = recipe;
            RequiredAmount = amount;
            MinimumQuality = quality;
            CreditReward = credits;
            ResearchReward = research;
            Description = description;
        }
    }

    public static class SemiconContractCatalog
    {
        public const int Count = 9;

        private static readonly SemiconContractDefinition[] Definitions =
        {
            new(SemiconContractKind.OxideEvaluation, "OXV-02", "산화막 평가 웨이퍼", "한빛 소재 연구소",
                SemiconRecipeKind.OxidizedWafer, 2, 80, 6500, 6, "절연막 두께와 균일도를 검증할 산화 웨이퍼 샘플"),
            new(SemiconContractKind.PhotoMaskVerification, "PHV-03", "마스크 검증 웨이퍼", "네오 마스크 시스템",
                SemiconRecipeKind.PhotoPatternedWafer, 2, 82, 8000, 8, "신규 회로 마스크 정합도를 확인하기 위한 패턴 웨이퍼"),
            new(SemiconContractKind.EtchProfileSample, "ETV-04", "식각 프로파일 샘플", "정밀 플라즈마 연구원",
                SemiconRecipeKind.EtchedWafer, 2, 84, 10000, 10, "측벽 형상과 식각 깊이를 평가할 공정 샘플"),
            new(SemiconContractKind.ThinFilmUniformity, "DPV-05", "박막 균일도 샘플", "미래 박막 솔루션",
                SemiconRecipeKind.DepositedWafer, 2, 86, 13000, 12, "단차 피복성과 막 두께 균일도를 검증할 증착 웨이퍼"),
            new(SemiconContractKind.MetalResistanceTest, "MTV-06", "배선 저항 테스트 웨이퍼", "코어 인터커넥트",
                SemiconRecipeKind.MetalizedWafer, 2, 88, 17000, 14, "저저항 금속 배선 성능 검증용 웨이퍼"),
            new(SemiconContractKind.KnownGoodDie, "KGD-07", "선별 합격 다이", "세이프칩 패키징",
                SemiconRecipeKind.TestedWafer, 2, 90, 22000, 18, "전기 검사에서 선별된 고신뢰성 패키징 투입품"),
            new(SemiconContractKind.Sc01IndustrialSensor, "SC-01", "산업용 센서 제어칩", "아틀라스 오토메이션",
                SemiconRecipeKind.Sc01ControlSensor, 3, 85, 28000, 10, "공장 센서 모듈에 탑재할 범용 제어 반도체"),
            new(SemiconContractKind.Pm10PowerManagement, "PM-10", "전력 관리 IC", "볼트웍스 에너지",
                SemiconRecipeKind.Pm10PowerManagement, 3, 88, 38000, 14, "산업 장비의 안정적인 전력 분배를 담당하는 고효율 IC"),
            new(SemiconContractKind.Dd20DisplayDriver, "DD-20", "디스플레이 드라이버", "루멘 디스플레이",
                SemiconRecipeKind.Dd20DisplayDriver, 4, 90, 55000, 18, "정밀 패턴과 균일한 출력이 필요한 디스플레이 구동 IC")
        };

        public static SemiconContractDefinition Get(SemiconContractKind kind)
        {
            var index = (int)kind;
            return index >= 0 && index < Definitions.Length ? Definitions[index] : default;
        }

        public static SemiconContractDefinition GetAt(int index)
        {
            return index >= 0 && index < Definitions.Length ? Definitions[index] : default;
        }

        public static bool IsSample(SemiconContractKind kind) => (int)kind >= 0 && (int)kind <= 5;
    }
}
