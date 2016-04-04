using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FortuneTellerService.Models
{
    public static class SampleData
    {
        internal static async Task InitializeFortunesAsync(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var db = serviceScope.ServiceProvider.GetService<FortuneContext>();

                if (await db.Database.EnsureCreatedAsync())
                {
                    await InsertFortunes(db);

                }
            }

        }

        private static async Task InsertFortunes(FortuneContext db)
        {
            var fortunes = GetFortunes();
            foreach(var fortune in fortunes)
            {
                db.Fortunes.Add(fortune);
            }
            await db.SaveChangesAsync();
        }

        private static Fortune[] GetFortunes()
        {
            var fortunes = new Fortune[]
            {
                new Fortune { Id =1000, Text="People are naturally attracted to you."},
                new Fortune { Id =1001, Text="You learn from your mistakes... You will learn a lot today."},
                new Fortune { Id =1002, Text="If you have something good in your life, don't let it go!"},
                new Fortune { Id =1003, Text="What ever you're goal is in life, embrace it visualize it, and for it will be yours."},
                new Fortune { Id =1004, Text="Your shoes will make you happy today."},
                new Fortune { Id =1005, Text="You cannot love life until you live the life you love."},
                new Fortune { Id =1006, Text="Be on the lookout for coming events; They cast their shadows beforehand."},
                new Fortune { Id =1007, Text="Land is always on the mind of a flying bird."},
                new Fortune { Id =1008, Text="The man or woman you desire feels the same about you."},
                new Fortune { Id =1009, Text="Meeting adversity well is the source of your strength."},
                new Fortune { Id =1010, Text="A dream you have will come true."},
                new Fortune { Id =1011, Text="Our deeds determine us, as much as we determine our deeds."},
                new Fortune { Id =1012, Text="Never give up. You're not a failure if you don't give up."},
                new Fortune { Id =1013, Text="You will become great if you believe in yourself."},
                new Fortune { Id =1014, Text="There is no greater pleasure than seeing your loved ones prosper."},
                new Fortune { Id =1015, Text="You will marry your lover."},
                new Fortune { Id =1016, Text="A very attractive person has a message for you."},
                new Fortune { Id =1017, Text="You already know the answer to the questions lingering inside your head."},
                new Fortune { Id =1018, Text="It is now, and in this world, that we must live."},
                new Fortune { Id =1019, Text="You must try, or hate yourself for not trying."},
                new Fortune { Id =1020, Text="You can make your own happiness."},
                new Fortune { Id =1021, Text="The greatest risk is not taking one."},
                new Fortune { Id =1022, Text="The love of your life is stepping into your planet this summer."},
                new Fortune { Id =1023, Text="Love can last a lifetime, if you want it to."},
                new Fortune { Id =1024, Text="Adversity is the parent of virtue."},
                new Fortune { Id =1025, Text="Serious trouble will bypass you."},
                new Fortune { Id =1026, Text="A short stranger will soon enter your life with blessings to share."},
                new Fortune { Id =1027, Text="Now is the time to try something new."},
                new Fortune { Id =1028, Text="Wealth awaits you very soon."},
                new Fortune { Id =1029, Text="If you feel you are right, stand firmly by your convictions."},
                new Fortune { Id =1030, Text="If winter comes, can spring be far behind?"},
                new Fortune { Id =1031, Text="Keep your eye out for someone special."},
                new Fortune { Id =1032, Text="You are very talented in many ways."},
                new Fortune { Id =1033, Text="A stranger, is a friend you have not spoken to yet."},
                new Fortune { Id =1034, Text="A new voyage will fill your life with untold memories."},
                new Fortune { Id =1035, Text="You will travel to many exotic places in your lifetime."},
                new Fortune { Id =1036, Text="Your ability for accomplishment will follow with success."},
                new Fortune { Id =1037, Text="Nothing astonishes men so much as common sense and plain dealing."},
                new Fortune { Id =1038, Text="Its amazing how much good you can do if you dont care who gets the credit."},
                new Fortune { Id =1039, Text="Everyone agrees. You are the best."},
                new Fortune { Id =1040, Text="LIFE CONSIST NOT IN HOLDING GOOD CARDS, BUT IN PLAYING THOSE YOU HOLD WELL."},
                new Fortune { Id =1041, Text="Jealousy doesn''t open doors, it closes them!"},
                new Fortune { Id =1042, Text="It''s better to be alone sometimes."},
                new Fortune { Id =1043, Text="When fear hurts you, conquer it and defeat it!"},
                new Fortune { Id =1044, Text="Let the deeds speak."},
                new Fortune { Id =1045, Text="You will be called in to fulfill a position of high honor and responsibility."},
                new Fortune { Id =1046, Text="The man on the top of the mountain did not fall there."},
                new Fortune { Id =1047, Text="You will conquer obstacles to achieve success."},
                new Fortune { Id =1048, Text="Joys are often the shadows, cast by sorrows."},
                new Fortune { Id =1049, Text="Fortune favors the brave."},

            };

            return fortunes;
        }
    }
}

