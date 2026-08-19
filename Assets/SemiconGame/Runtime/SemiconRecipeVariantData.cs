using System;
using UnityEngine;

namespace SemiconCity.Game
{
    [Serializable]
    public sealed class SemiconRecipeVariantData
    {
        public SemiconRecipeKind recipe;
        public int serial;
        public int primaryParameter;
        public float secondaryParameter;
        public float metricA;
        public float metricB;
        public float metricC;
        [Range(1, 100)] public int qualityScore;

        public string Grade => qualityScore >= 96 ? "S" : qualityScore >= 92 ? "A" : qualityScore >= 88 ? "B" : "C";

        public string StyleName => qualityScore >= 96
            ? "고품질형"
            : qualityScore >= 92
                ? "안정형"
                : "표준형";

        public string DisplayCode => $"{GetBaseCode(recipe)}-{Mathf.Max(1, serial):00}";

        public string ShortLabel => $"{DisplayCode}  ·  {Grade}등급  ·  품질 {qualityScore}";

        public string ParameterSummary => recipe switch
        {
            SemiconRecipeKind.OxidizedWafer => $"온도 {primaryParameter} ℃  ·  시간 {secondaryParameter:0} s",
            SemiconRecipeKind.PhotoPatternedWafer => $"노광량 {primaryParameter} mJ/cm²  ·  초점 {secondaryParameter:+0.00;-0.00;0.00} μm",
            SemiconRecipeKind.EtchedWafer => $"RF 파워 {primaryParameter} W  ·  가스 {secondaryParameter:0} sccm",
            SemiconRecipeKind.DepositedWafer => $"온도 {primaryParameter} ℃  ·  압력 {secondaryParameter:0} mTorr",
            SemiconRecipeKind.MetalizedWafer => $"파워 {primaryParameter} W  ·  시간 {secondaryParameter:0} s",
            SemiconRecipeKind.TestedWafer => $"검사 전압 {primaryParameter} V  ·  누설 기준 {secondaryParameter:0} μA",
            SemiconRecipeKind.Sc01ControlSensor => $"본딩 힘 {primaryParameter} N  ·  몰딩 온도 {secondaryParameter:0} ℃",
            _ => "기본 공정 조건"
        };

        public string ResultSummary => recipe switch
        {
            SemiconRecipeKind.OxidizedWafer => $"두께 {metricA:0.0} nm  ·  균일도 {metricB:0.0}%",
            SemiconRecipeKind.PhotoPatternedWafer => $"수율 {metricA:0.0}%  ·  정밀도 {metricB:0.0}%",
            SemiconRecipeKind.EtchedWafer => $"식각 깊이 {metricA:0.0} nm  ·  프로파일 {metricB:0.0}%",
            SemiconRecipeKind.DepositedWafer => $"두께 {metricA:0.0} nm  ·  균일도 {metricB:0.0}%  ·  피복률 {metricC:0.0}%",
            SemiconRecipeKind.MetalizedWafer => $"두께 {metricA:0.0} nm  ·  저항 {metricB:0.000} Ω  ·  접착력 {metricC:0.0}%",
            SemiconRecipeKind.TestedWafer => $"수율 {metricA:0.0}%  ·  검출률 {metricB:0.0}%  ·  오판정 {metricC:0.0}%",
            SemiconRecipeKind.Sc01ControlSensor => $"본딩 {metricA:0.0}%  ·  패키지 {metricB:0.0}%  ·  최종 합격 {metricC:0.0}%",
            _ => string.Empty
        };

        public static string GetBaseCode(SemiconRecipeKind recipe)
        {
            return recipe switch
            {
                SemiconRecipeKind.OxidizedWafer => "OXIDE",
                SemiconRecipeKind.PhotoPatternedWafer => "PHOTO",
                SemiconRecipeKind.EtchedWafer => "ETCH",
                SemiconRecipeKind.DepositedWafer => "DEPO",
                SemiconRecipeKind.MetalizedWafer => "METAL",
                SemiconRecipeKind.TestedWafer => "EDS",
                SemiconRecipeKind.Sc01ControlSensor => "PACKAGE",
                _ => "WAFER"
            };
        }
    }
}
