using PearlDivers.Models;
using System;
using System.Windows.Media;

namespace PearlDivers.Engine
{
    public abstract class EnemyFactory
    {
        public abstract Enemy CreateEnemy(float x, float y, float speed, float minY, float maxY);
    }

    public class FishFactory : EnemyFactory
    {
        private readonly Random _random = new Random();

        public override Enemy CreateEnemy(float x, float y, float speed, float minY, float maxY)
        {
            string spriteName = $"fish_{_random.Next(1, 4)}";
            return new Enemy(x, y, EnemyType.Fish, speed, spriteName, minY, maxY);
        }
    }

    public class SnakeFactory : EnemyFactory
    {
        private readonly Random _random = new Random();

        public override Enemy CreateEnemy(float x, float y, float speed, float minY, float maxY)
        {
            string spriteName = $"snake_{_random.Next(1, 4)}";
            var snake = new Enemy(x, y, EnemyType.Snake, speed, spriteName, minY, maxY);

            snake.CenterOffsetY = 0f;
            snake.HeightShrink = 20f;


            return snake;
        }
    }

    public abstract class BonusFactory
    {
        public abstract Bonus CreateBonus(float x, float y);
    }

    public class HealBonusFactory : BonusFactory
    {
        public override Bonus CreateBonus(float x, float y) => new Bonus(x, y, BonusType.Heal);
    }

    public class OxygenBonusFactory : BonusFactory
    {
        public override Bonus CreateBonus(float x, float y) => new Bonus(x, y, BonusType.Oxygen);
    }

    public class ShieldBonusFactory : BonusFactory
    {
        public override Bonus CreateBonus(float x, float y) => new Bonus(x, y, BonusType.Shield);
    }
}