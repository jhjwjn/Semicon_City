using System.Collections.Generic;
using UnityEngine;

namespace SemiconCity.Game
{
    public static class SemiconGachaArt
    {
        private static Texture2D robotAtlas;
        private static Texture2D diskAtlas;
        private static readonly Dictionary<SemiconRobotKind, Sprite> RobotSprites = new();
        private static readonly Dictionary<int, Sprite> DiskSprites = new();

        public static Sprite GetRobotSprite(SemiconRobotKind robot)
        {
            if (robot == SemiconRobotKind.None) return null;
            if (RobotSprites.TryGetValue(robot, out var cached)) return cached;
            robotAtlas ??= Resources.Load<Texture2D>("Gacha/RobotAtlas");
            if (robotAtlas == null) return null;

            var index = (int)robot - 1;
            var column = index % 5;
            var rowFromTop = index / 5;
            var width = robotAtlas.width / 5f;
            var height = robotAtlas.height / 3f;
            var rect = new Rect(column * width, robotAtlas.height - (rowFromTop + 1) * height, width, height);
            var sprite = Sprite.Create(robotAtlas, rect, new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
            sprite.name = "Robot_" + robot;
            RobotSprites[robot] = sprite;
            return sprite;
        }

        public static Sprite GetDiskSprite(SemiconDiskKind disk, SemiconDiskGrade grade)
        {
            if (disk == SemiconDiskKind.None || grade == SemiconDiskGrade.None) return null;
            var key = (int)disk * 10 + (int)grade;
            if (DiskSprites.TryGetValue(key, out var cached)) return cached;
            diskAtlas ??= Resources.Load<Texture2D>("Gacha/DiskAtlas");
            if (diskAtlas == null) return null;

            var column = (int)disk - 1;
            var rowFromTop = (int)grade - 1;
            var width = diskAtlas.width / 3f;
            var height = diskAtlas.height / 3f;
            var rect = new Rect(column * width, diskAtlas.height - (rowFromTop + 1) * height, width, height);
            var sprite = Sprite.Create(diskAtlas, rect, new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
            sprite.name = $"Disk_{disk}_{grade}";
            DiskSprites[key] = sprite;
            return sprite;
        }

        public static Color GetRobotRarityColor(SemiconRobotKind robot)
        {
            return SemiconFactoryDefinitions.GetRobot(robot).Rarity switch
            {
                SemiconRobotRarity.SR => new Color32(247, 169, 30, 255),
                SemiconRobotRarity.R => new Color32(42, 216, 211, 255),
                _ => new Color32(146, 174, 181, 255)
            };
        }

        public static Color GetDiskGradeColor(SemiconDiskGrade grade)
        {
            return grade switch
            {
                SemiconDiskGrade.III => new Color32(247, 169, 30, 255),
                SemiconDiskGrade.II => new Color32(42, 216, 211, 255),
                _ => new Color32(146, 174, 181, 255)
            };
        }
    }
}
