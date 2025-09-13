//using System.Net.Http.Json;
//using Microsoft.EntityFrameworkCore;
//using Sommerhus.Api.Models;

//namespace Sommerhus.Api.Data;

//public static class Seed
//{
//    // DTO som matcher Dataforsyningen (https://api.dataforsyningen.dk/postnumre)
//    private record DkZipDto(string nr, string navn);

//    public static async Task EnsureSeedDataAsync(AppDbContext db)
//    {
//        // 1) Postnumre
//        if (!await db.ZipCodes.AnyAsync())
//        {
//            try
//            {
//                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
//                var data = await http.GetFromJsonAsync<List<DkZipDto>>("https://api.dataforsyningen.dk/postnumre");
//                if (data is not null)
//                {
//                    foreach (var z in data)
//                        db.ZipCodes.Add(new ZipCode { Zip = z.nr, City = z.navn });
//                    await db.SaveChangesAsync();
//                }
//            }
//            catch
//            {
//                // fallback hvis offline
//                db.ZipCodes.AddRange(
//                    new ZipCode { Zip = "2000", City = "Frederiksberg" },
//                    new ZipCode { Zip = "2100", City = "København Ø" },
//                    new ZipCode { Zip = "2200", City = "København N" },
//                    new ZipCode { Zip = "2300", City = "København S" },
//                    new ZipCode { Zip = "9990", City = "Skagen" }
//                );
//                await db.SaveChangesAsync();
//            }
//        }

//        // 2) Features (fleksible)
//        if (!await db.Features.AnyAsync())
//        {
//            db.Features.AddRange(
//                new Feature { Name = "Pool", Icon = "pool", ValueType = FeatureValueType.Bool },
//                new Feature { Name = "Have", Icon = "garden", ValueType = FeatureValueType.Bool },
//                new Feature { Name = "WiFi", Icon = "wifi", ValueType = FeatureValueType.Bool },
//                new Feature { Name = "Sauna", Icon = "sauna", ValueType = FeatureValueType.Bool },
//                new Feature { Name = "Afstand til vand", Icon = "water", ValueType = FeatureValueType.Decimal, Unit = "m" },
//                new Feature { Name = "Sovepladser", Icon = "bed", ValueType = FeatureValueType.Int, Unit = "pers." },
//                new Feature { Name = "Kæledyr tilladt", Icon = "pet", ValueType = FeatureValueType.Bool }
//            );
//            await db.SaveChangesAsync();
//        }

//        // 3) Demo-huse
//        if (!await db.VacationHouses.AnyAsync())
//        {
//            var f = await db.Features.ToListAsync();
//            int Fid(string name) => f.First(x => x.Name == name).Id;

//            var h1 = new VacationHouse
//            {
//                Id = Guid.NewGuid(),
//                Title = "Sommerhus ved klitterne",
//                Subtitle = "Hyggeligt og lyst",
//                Description = "Dejligt sommerhus tæt på stranden. Plads til familien.",
//                Address = "Klittevej 12",
//                City = "Skagen",
//                Zip = "9990",
//                Images = new List<VacationImage>
//                {
//                    new() { Url = "https://picsum.photos/seed/house1/1200/800", SortOrder = 1 },
//                    new() { Url = "https://picsum.photos/seed/house1b/1200/800", SortOrder = 2 }
//                }
//            };
//            h1.HouseFeatures.Add(new HouseFeature { VacationHouseId = h1.Id, FeatureId = Fid("WiFi"), ValueBool = true });
//            //h1.HouseFeatures.Add(new HouseFeature { VacationHouseId = h1.Id, FeatureId = Fid("Afstand til vand"), ValueDecimal = 120m });
//            h1.HouseFeatures.Add(new HouseFeature { VacationHouseId = h1.Id, FeatureId = Fid("Sovepladser"), ValueInt = 6 });

//            var h2 = new VacationHouse
//            {
//                Id = Guid.NewGuid(),
//                Title = "Poolhus i skoven",
//                Subtitle = "Familievenligt",
//                Description = "Stor grund, indendørs pool og sauna.",
//                Address = "Skovvej 3",
//                City = "Frederiksberg",
//                Zip = "2000",
//                Images = new List<VacationImage>
//                {
//                    new() { Url = "https://picsum.photos/seed/house2/1200/800", SortOrder = 1 }
//                }
//            };
//            h2.HouseFeatures.Add(new HouseFeature { VacationHouseId = h2.Id, FeatureId = Fid("Pool"), ValueBool = true });
//            h2.HouseFeatures.Add(new HouseFeature { VacationHouseId = h2.Id, FeatureId = Fid("Sauna"), ValueBool = true });
//            h2.HouseFeatures.Add(new HouseFeature { VacationHouseId = h2.Id, FeatureId = Fid("Kæledyr tilladt"), ValueBool = true });
//            h2.HouseFeatures.Add(new HouseFeature { VacationHouseId = h2.Id, FeatureId = Fid("Sovepladser"), ValueInt = 8 });
//            h2.HouseFeatures.Add(new HouseFeature { VacationHouseId = h2.Id, FeatureId = Fid("Afstand til vand"), ValueDecimal = 2500m });

//            db.VacationHouses.AddRange(h1, h2);
//            await db.SaveChangesAsync();
//        }
//    }
//}
