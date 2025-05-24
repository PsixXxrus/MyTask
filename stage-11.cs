using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BattlePass", "PsixXx", "0.0.1")]
    internal class BattlePass : RustPlugin
    {
        #region Static
        private Coroutine _coroutine;
        private readonly Dictionary<string, string> ImageList = new Dictionary<string, string>();
        private readonly List<BaseEntity> IgnoredContainers = new List<BaseEntity>();
        private readonly Dictionary<ulong, ulong> LastHeliHit = new Dictionary<ulong, ulong>();
        private Configuration _config;
        private const bool isEn = false;

        #endregion

        #region Config

        private class Configuration
        {
            [JsonProperty(PropertyName = isEn ? "Picture for regular awards" : "Картинка для обычных наград", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly string DefaultImage = "https://i.imgur.com/R55go13.png";

            [JsonProperty(PropertyName = isEn ? "Permission to access Default rewards" : "Пермишн для доступа к обычным наградам", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly string DefaultRewardPermission = "battlepass.default";

            [JsonProperty(PropertyName = isEn ? "Notification effect(leave blank if not needed)" : "Эффект уведомления(оставить пустым если не нужно)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly string Effect = "assets/prefabs/misc/halloween/lootbag/effects/gold_open.prefab";

            [JsonProperty(PropertyName = isEn ? "Notify the player of a new level?" : "Уведомлять игрока о новом уровне?", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly bool EnableNotification = true;

            [JsonProperty(PropertyName = isEn ? "Setting up points" : "Настройка поинтов", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly PointSettings Points = new PointSettings();

            [JsonProperty(PropertyName = isEn ? "Picture for VIP awards" : "Картинка для VIP наград", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly string VipImage = "https://i.imgur.com/chknZZf.png";

            [JsonProperty(PropertyName = isEn ? "Permission to access VIP rewards" : "Пермишн для доступа к VIP наградам", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly string VipRewardPermission = "battlepass.vip";

            [JsonProperty(PropertyName = isEn ? "List of commands to open the interface" : "Список команд для открытия интерфейса", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly List<string> CmdList = new List<string>
            {
                "bp",
                "bpass",
                "pass",
                "battlepass"
            };


            [JsonProperty(PropertyName = isEn ? "Setting up normal levels" : "Настройка обычных уровней", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly List<LevelSettings> DefaultLevelList = new List<LevelSettings>
            {
                new LevelSettings
                {
                    LevelID = 1,
                    ExpForLevel = 20,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "hatchet",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 2,
                    ExpForLevel = 30,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "pistol.nailgun",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.nailgun.nails",
                            Amount = 60,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 3,
                    ExpForLevel = 40,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "shirt.collared",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "pants.shorts",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 4,
                    ExpForLevel = 50,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "wood.armor.jacket",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "wood.armor.pants",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "wood.armor.helmet",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 5,
                    ExpForLevel = 60,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "scrap",
                            Amount = 100,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 6,
                    ExpForLevel = 70,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "grenade.beancan",
                            Amount = 10,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 7,
                    ExpForLevel = 80,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "pistol.revolver",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.pistol",
                            Amount = 60,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 8,
                    ExpForLevel = 90,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "door.double.hinged.metal",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 9,
                    ExpForLevel = 100,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "syringe.medical",
                            Amount = 20,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 10,
                    OutlineEnable = true,
                    ExpForLevel = 110,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "easter.bronzeegg",
                            Amount = 20,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 11,
                    ExpForLevel = 120,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "small.oil.refinery",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 12,
                    ExpForLevel = 130,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "pistol.python",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.pistol",
                            Amount = 60,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 13,
                    ExpForLevel = 140,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "wall.external.high",
                            Amount = 10,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "gates.external.high.wood",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 14,
                    ExpForLevel = 150,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "metal.fragments",
                            Amount = 3000,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 15,
                    ExpForLevel = 160,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "smg.thompson",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.pistol",
                            Amount = 60,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 16,
                    ExpForLevel = 170,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "wall.frame.garagedoor",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 17,
                    ExpForLevel = 180,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "explosive.satchel",
                            Amount = 10,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 18,
                    ExpForLevel = 190,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "chainsaw",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "lowgradefuel",
                            Amount = 100,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 19,
                    ExpForLevel = 200,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "sulfur",
                            Amount = 5000,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 20,
                    OutlineEnable = true,
                    ExpForLevel = 220,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "xmas.present.large",
                            Amount = 10,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 21,
                    ExpForLevel = 240,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "multiplegrenadelauncher",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 22,
                    ExpForLevel = 260,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.grenadelauncher.he",
                            Amount = 20,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 23,
                    ExpForLevel = 280,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.rifle",
                            Amount = 200,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 24,
                    ExpForLevel = 300,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "rifle.bolt",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.rifle",
                            Amount = 200,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 25,
                    ExpForLevel = 350,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "hmlmg",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.rifle",
                            Amount = 300,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 26,
                    ExpForLevel = 400,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.rocket.hv",
                            Amount = 30,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 27,
                    ExpForLevel = 450,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "electric.generator.small",
                            Amount = 2,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 28,
                    ExpForLevel = 500,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "gunpowder",
                            Amount = 3000,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 29,
                    ExpForLevel = 550,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "metal.fragments",
                            Amount = 5000,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 30,
                    OutlineEnable = true,
                    ExpForLevel = 600,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "easter.goldegg",
                            Amount = 10,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                }
            };


            [JsonProperty(PropertyName = isEn ? "Setting up vip levels" : "Настройка VIP уровней", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public readonly List<LevelSettings> VipLevelList = new List<LevelSettings>
            {
                new LevelSettings
                {
                    LevelID = 1,
                    ExpForLevel = 20,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "axe.salvaged",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 2,
                    ExpForLevel = 30,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "pistol.revolver",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.pistol",
                            Amount = 60,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 3,
                    ExpForLevel = 40,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "hoodie",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "pants",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 4,
                    ExpForLevel = 50,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "roadsign.jacket",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "roadsign.kilt",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "coffeecan.helmet",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 5,
                    ExpForLevel = 60,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "scrap",
                            Amount = 300,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 6,
                    ExpForLevel = 70,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "explosive.satchel",
                            Amount = 5,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 7,
                    ExpForLevel = 80,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "pistol.semiauto",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.pistol",
                            Amount = 60,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 8,
                    ExpForLevel = 90,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "wall.frame.garagedoor",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 9,
                    ExpForLevel = 100,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "largemedkit",
                            Amount = 10,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 10,
                    ExpForLevel = 110,
                    OutlineEnable = true,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "easter.silveregg",
                            Amount = 10,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 11,
                    ExpForLevel = 120,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "furnace.large",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 12,
                    ExpForLevel = 130,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "pistol.m92",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.pistol",
                            Amount = 60,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 13,
                    ExpForLevel = 140,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "wall.external.high.stone",
                            Amount = 10,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "gates.external.high.stone",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 14,
                    ExpForLevel = 150,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "metal.refined",
                            Amount = 400,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 15,
                    ExpForLevel = 160,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "smg.mp5",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.pistol",
                            Amount = 60,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 16,
                    ExpForLevel = 170,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "door.double.hinged.toptier",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 17,
                    ExpForLevel = 180,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "explosive.timed",
                            Amount = 10,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 18,
                    ExpForLevel = 190,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "jackhammer",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 19,
                    ExpForLevel = 200,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "gunpowder",
                            Amount = 3000,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 20,
                    OutlineEnable = true,
                    ExpForLevel = 220,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "xmas.present.large",
                            Amount = 20,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 21,
                    ExpForLevel = 240,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "rocket.launcher",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 22,
                    ExpForLevel = 260,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.rocket.basic",
                            Amount = 20,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 23,
                    ExpForLevel = 280,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "rifle.lr300",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.rifle",
                            Amount = 200,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 24,
                    ExpForLevel = 300,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "rifle.l96",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.rifle",
                            Amount = 200,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 25,
                    ExpForLevel = 350,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "lmg.m249",
                            Amount = 1,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.rifle",
                            Amount = 300,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 26,
                    ExpForLevel = 400,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "ammo.rifle.explosive",
                            Amount = 500,
                            SkinID = 0,

                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 27,
                    ExpForLevel = 450,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "autoturret",
                            Amount = 5,
                            SkinID = 0,
                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 28,
                    ExpForLevel = 500,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "explosives",
                            Amount = 300,
                            SkinID = 0,
                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 29,
                    ExpForLevel = 550,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "metal.refined",
                            Amount = 1000,
                            SkinID = 0,
                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                },
                new LevelSettings
                {
                    LevelID = 30,
                    OutlineEnable = true,
                    ExpForLevel = 600,
                    Image = "",
                    RewardList = new List<LevelSettings.RewardSettings>
                    {
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "halloween.lootbag.large",
                            Amount = 20,
                            SkinID = 0,
                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        },
                        new LevelSettings.RewardSettings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            SkinID = 0,
                            CmdList = new List<string>
                            {
                                "o.grant user %STEAMID% vip",
                                "o.grant user %STEAMID% elite"
                            }
                        }
                    }
                }
            };

            internal class PointSettings
            {
                [JsonProperty(isEn ? "Points for winning the Air Event (if available)" : "Очки за победу в ивенте Air Event(при наличии)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly int AirEvent = 10;


                [JsonProperty(isEn ? "Points for winning the Arctic Base Event (if available)" : "Очки за победу в ивенте Arctic Base Event(при наличии)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly int ArcticBaseEvent = 10;


                [JsonProperty(isEn ? "Points for killing Boss Monster (if available)" : "Очки за убийство Boss Monster(при наличии)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly int BossMonster = 5;

                [JsonProperty(isEn ? "Setting up points for crafting items (item-points)" : "Настрока очков за крафт предметов(предмет-поинты)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly Dictionary<string, int> CraftItemList = new Dictionary<string, int>
                {
                    ["rifle.ak"] = 10,
                    ["pickaxe"] = 2,
                    ["explosive.timed"] = 10
                };

                [JsonProperty(isEn ? "The number of points deducted for death" : "Количество очков отнимаемое за смерть", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly int DeathPoint = 2;

                [JsonProperty(isEn ? "Setting up gather" : "Настройка добычи", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly GatherSettings Gather = new GatherSettings();

                [JsonProperty(isEn ? "Points for winning the Harbor Event (if available)" : "Очки за победу в ивенте Harbor Event (при наличии)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly int HarborEvent = 10;

                [JsonProperty(isEn ? "Points for winning the Junkyard Event (if available)" : "Очки за победу в ивенте Junkyard Event(при наличии)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly int JunkyardEvent = 10;

                [JsonProperty(isEn ? "The number of points for the destruction of the helicopter" : "Количество очков за уничтожение вертолета", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly int PatrolHeliScore = 5;

                [JsonProperty(isEn ? "Bonus Point Multiplier" : "Множитель очков с привилегиями", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly Dictionary<string, float> PermissionList = new Dictionary<string, float>
                {
                    ["battlepass.default"] = 1.0f,
                    ["battlepass.vip"] = 1.5f,
                    ["battlepass.elite"] = 2.0f
                };

                [JsonProperty(isEn ? "Points for winning the Plant Event (if available)" : "Очки за победу в ивенте Power Plant Event(при наличии)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly int PowerPlantEvent = 10;


                [JsonProperty(isEn ? "Setting up points for destroying/killing/exploding objects (ShortPrefabName-Points)" : "Настройка очков за разрушение/убийство/взрыв объектов(ShortPrefabName-Поинты)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly Dictionary<string, int> RaidObjects = new Dictionary<string, int>
                {
                    ["cupboard.tool.deployed"] = 10,
                    ["floor"] = 2,
                    ["loot_barrel_1"] = 2,
                    ["door.hinged.metal"] = 5,
                    ["boar"] = 2,
                    ["bear"] = 4,
                    ["scientistnpc_cargo"] = 5,
                    ["scientistnpc_junkpile_pistol"] = 2,
                    ["player"] = 2,
                    ["bradleyapc"] = 10
                };

                [JsonProperty(isEn ? "Points for winning the Satellite Dish Event (if available)" : "Очки за победу в ивенте Satellite Dish Event(при наличии)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly int SatelliteDishEvent = 10;

                [JsonProperty(isEn ? "Points for winning the Water Event (if available)" : "Очки за победу в ивенте Water Event (при наличии)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly int WaterEvent = 10;

                [JsonProperty(PropertyName = isEn ? "Issuance of points for completing quests XDQuests" : "Выдача очков за выполнения квестов XDQuests", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public readonly Dictionary<string, int> XDQuestsPoints = new Dictionary<string, int>
                {
                    ["QUESTDISPLAYNAME"] = 1,
                    ["QUESTDISPLAYNAM2"] = 1
                };

                internal class GatherSettings
                {
                    [JsonProperty(isEn ? "Setting up gather(shortname-points)" : "Настройка добычи(shortname-поинты)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public readonly Dictionary<string, int> GatherList = new Dictionary<string, int>
                    {
                        ["wood"] = 2,
                        ["stones"] = 2,
                        ["sulfur-ore"] = 2,
                        ["metal-ore"] = 2
                    };

                    [JsonProperty(isEn ? "Setting up points for opening boxes (box-points)" : "Настройка очков за открытие ящиков(ящик-поинты)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public readonly Dictionary<string, int> LootBoxList = new Dictionary<string, int>
                    {
                        ["crate_elite"] = 5,
                        ["crate_normal"] = 2,
                        ["crate_normal_2"] = 1,
                        ["codelockedhackablecrate"] = 10
                    };

                    [JsonProperty(isEn ? "Setting up collected items (shortname-points)" : "Настройка подымаемых предметов(shortname-поинты)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public readonly Dictionary<string, int> PickupItemList = new Dictionary<string, int>
                    {
                        ["wood"] = 2,
                        ["stones"] = 2,
                        ["sulfur-ore"] = 2,
                        ["metal-ore"] = 2
                    };
                }
            }

            internal class LevelSettings
            {
                [JsonProperty(isEn ? "The amount of EXP to get this level" : "Количество EXP для получения этого уровня", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public int ExpForLevel = 20;

                [JsonProperty(isEn ? "Reward display picture (if empty, takes 1 picture from the reward)" : "Картинка отображения награды(если пустое, берет 1 картинку из награды)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string Image = "";

                [JsonProperty(isEn ? "Level number" : "Номер уровня", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public int LevelID = 1;

                [JsonProperty(isEn ? "Highlight the award in green?" : "Выделить награду зеленым цветом?", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public bool OutlineEnable;

                [JsonProperty(isEn ? "Level Reward" : "Награда за уровень", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<RewardSettings> RewardList = new List<RewardSettings>
                {
                    new RewardSettings(),
                    new RewardSettings(),
                    new RewardSettings()
                };

                internal class RewardSettings
                {
                    [JsonProperty("Amount", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public int Amount = 1;


                    [JsonProperty(PropertyName = "Display Name", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public string displayName = "" ; [JsonProperty(isEn ? "Commands to be executed (Use %STEAMID% to enter the player's steam ID)" : "Команды которые должны выполняться(Используйте %STEAMID% для ввода стимИД игрока)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public List<string> CmdList = new List<string>
                    {
                        "o.grant user %STEAMID% vip",
                        "o.grant user %STEAMID% elite"
                    };

                    [JsonProperty(PropertyName = "Is blueprint?", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public readonly bool isBlueprint = false;

                    [JsonProperty("ShortName", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public string ShortName = "scrap";

                    [JsonProperty("SkinID", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                    public ulong SkinID;
                }
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
        }

        #endregion

        #region OxideHooks


        private void OnServerInitialized()
        {

            permission.RegisterPermission(_config.VipRewardPermission, this);
            permission.RegisterPermission(_config.DefaultRewardPermission, this);
            foreach (var check in _config.Points.PermissionList) permission.RegisterPermission(check.Key, this);
            foreach (var check in _config.CmdList) cmd.AddChatCommand(check, this, nameof(cmdChat));
            PrintError("|-----------------------------------|");
            PrintWarning($"|  Plugin {Title} v{Version} is loaded  |");
            PrintWarning("|          Discord: CASHR#6906      |");
            PrintError("|-----------------------------------|");
            ImageSettingsList.Add(new ImageSettings()
            {
                Name = _config.DefaultImage,
                Url = _config.DefaultImage,
            });
            ImageSettingsList.Add(new ImageSettings()
            {
                Name = _config.VipImage,
                Url = _config.VipImage,
            });
            foreach (var check in _config.DefaultLevelList)
            {
                if (string.IsNullOrEmpty(check.Image))
                {
                    var image = check.RewardList.FirstOrDefault(p => !string.IsNullOrEmpty(p.ShortName)).ShortName;
                    check.Image = $"https://www.rustedit.io/images/imagelibrary/{image}.png";
                }
                ImageSettingsList.Add(new ImageSettings()
                {
                    Name = check.Image,
                    Url = check.Image
                });
            }
            foreach (var check in _config.VipLevelList)
            {
                if (string.IsNullOrEmpty(check.Image))
                {
                    var image = check.RewardList.FirstOrDefault(p => !string.IsNullOrEmpty(p.ShortName)).ShortName;
                    check.Image = $"https://www.rustedit.io/images/imagelibrary/{image}.png";
                }
                ImageSettingsList.Add(new ImageSettings()
                {
                    Name = check.Image,
                    Url = check.Image
                });
            }
            ImageSettingsList.Add(new ImageSettings()
            {
                Name = "blueprintbase",
                Url = "https://www.rustedit.io/images/imagelibrary/blueprintbase.png"
            });
            SaveConfig();
            PrintWarning("The loading of images begins");
            DownloadImage();
        }

        private void OnRaidableBaseEnded(Vector3 raidPos, int difficulty, bool AllowPVP, string ID, float spawnTime, float despawnTime, float loadTime, ulong ownerId, BasePlayer owner, List<BasePlayer> Raiders, List<BasePlayer> Intruders, List<BaseEntity> Entities, string BaseName,
            DateTime spawnDateTime, DateTime despawnDateTime, float ProtectionRadius)
        {
            
        }

   
        private void OnQuestCompleted(BasePlayer player, string DisplayName)
        {
            var exp = 0;
            if (!_config.Points.XDQuestsPoints.TryGetValue(DisplayName, out exp)) return;
            GiveExp(player, exp);
        }

        private void OnArcticBaseEventWinner(ulong winnerId)
        {
            GiveExp(winnerId, _config.Points.ArcticBaseEvent);
        }

        private void OnSatDishEventWinner(ulong winnerId)
        {
            GiveExp(winnerId, _config.Points.SatelliteDishEvent);
        }

        private void OnBossKilled(ScientistNPC boss, BasePlayer attacker)
        {
            GiveExp(attacker.userID, _config.Points.BossMonster);
        }

        private void OnJunkyardEventWinner(ulong winnerId)
        {
            GiveExp(winnerId, _config.Points.JunkyardEvent);
        }

        private void OnPowerPlantEventWinner(ulong winnerId)
        {
            GiveExp(winnerId, _config.Points.PowerPlantEvent);
        }

        private void OnAirEventWinner(ulong winnerId)
        {
            GiveExp(winnerId, _config.Points.AirEvent);
        }

        private void OnHarborEventWinner(ulong winnerId)
        {
            GiveExp(winnerId, _config.Points.HarborEvent);
        }

        private void OnWaterEventWinner(ulong winnerId)
        {
            GiveExp(winnerId, _config.Points.WaterEvent);
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item, ItemCrafter crafter)
        {
            if(crafter == null)return;
            var player = crafter.owner;
            if (player == null) return;
            var exp = 0;
            if (!_config.Points.CraftItemList.TryGetValue(item.info.shortname, out exp)) return;
            GiveExp(player, exp);
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            var exp = 0;
            if (!_config.Points.Gather.GatherList.TryGetValue(item.info.shortname, out exp)) return;
            GiveExp(player, exp);
        }

        private void OnEntityDeath(PatrolHelicopter entity, HitInfo info)
        {
            if (!LastHeliHit.ContainsKey(entity.net.ID.Value)) return;
            GiveExp(LastHeliHit[entity.net.ID.Value], _config.Points.PatrolHeliScore);
        }

        private object OnEntityTakeDamage(PatrolHelicopter entity, HitInfo info)
        {
            if (entity == null || info == null || info.InitiatorPlayer == null) return null;
            var player = info.InitiatorPlayer;
            if (!LastHeliHit.ContainsKey(entity.net.ID.Value))
                LastHeliHit.Add(entity.net.ID.Value, player.userID);
            LastHeliHit[entity.net.ID.Value] = player.userID;
            return null;
        }

        private object OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            if (collectible == null || collectible.itemList == null) return null;
            for (var index = 0; index < collectible.itemList.Length; index++)
            {
                var check = collectible.itemList[index];
                if (check.itemDef == null) continue;
                var exp = 0;
                if (!_config.Points.Gather.PickupItemList.TryGetValue(check.itemDef.shortname, out exp)) continue;
                GiveExp(player, exp);
            }

            return null;
        }

        private void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            if (entity == null || info == null || info.InitiatorPlayer == null) return;
            var player = info.InitiatorPlayer;
            if (entity.OwnerID == player.userID) return;
            var victim = entity as BasePlayer;
            if (victim != null)
                if (victim.userID.IsSteamId())
                {
                    RemoveExp(victim.userID, _config.Points.DeathPoint);
                    if (player.userID == victim.userID) return;
                }

            var exp = 0;

            if (!_config.Points.RaidObjects.TryGetValue(entity.ShortPrefabName, out exp)) return;

            GiveExp(player, exp);
        }


        private void OnLootEntity(BasePlayer player, StorageContainer container)
        {
            if (player == null || container == null || IgnoredContainers.Contains(container)) return;
            var exp = 0;
            if (!_config.Points.Gather.LootBoxList.TryGetValue(container.ShortPrefabName, out exp)) return;
            GiveExp(player, exp);
            IgnoredContainers.Add(container);
        }

        private void Unload()
        {
            if (_coroutine != null)
                ServerMgr.Instance.StopCoroutine(_coroutine);
            SaveData();
        }

        #endregion

        #region Data

        private Dictionary<ulong, Data> _data;

        private class Data
        {
            public readonly List<int> DefaultRewardID = new List<int>();
            public readonly List<int> VipRewardID = new List<int>();
            public int Level;
            public int Score;
        }

        private void LoadData()
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/PlayerData"))
                _data = new Dictionary<ulong, Data>();
            else
                _data = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Data>>(
                    $"{Name}/PlayerData");
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerData", _data);

            if (_data == null)
                _data = new Dictionary<ulong, Data>();


            foreach (var check in BasePlayer.activePlayerList) OnPlayerConnected(check);
        }

        private void Init()
        {
            LoadData();
        }

        private void OnServerSave()
        {
            SaveData();
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerData", _data);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!_data.ContainsKey(player.userID))
                _data.Add(player.userID, new Data());
        }

        #endregion

        #region Function

      
        private string GetImage(string url)=> ImageList[url];

        private List<ImageSettings> ImageSettingsList = new();



        private class ImageSettings
        {
            [JsonProperty(PropertyName = "Name", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string Name;

            [JsonProperty(PropertyName = "Path", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string Url;
        }

        private void DownloadImage()
        {
            var image = ImageSettingsList.FirstOrDefault(p => !ImageList.ContainsKey(p.Name));


            if (image == null)
            {
                PrintWarning("Image upload completed");
                return;
            }

            ServerMgr.Instance.StartCoroutine(StartDownloadImage(image));
        }

        private IEnumerator StartDownloadImage(ImageSettings image)
        {
           
            var url = image.Url;
            using (var www = new WWW(url))
            {
                yield return www;
                if (www.error != null)
                {
                    PrintError($"Failed to download image {image.Name}.Address [{image.Url}] invalid");
                    ImageList.Add(image.Name, "");
                }
                else
                {
                    var texture = www.texture;
                    var png = FileStorage.server.Store(texture.EncodeToPNG(), FileStorage.Type.png,
                        CommunityEntity.ServerInstance.net.ID).ToString();
                    ImageList.Add(image.Name, png);
                }

                DownloadImage();
            }
        }

        private void cmdChat(BasePlayer player, string command, string[] args)
        {
            ShowMainUI(player);
        }

        [ConsoleCommand("UI_BATTLEPASS_PAGE")]
        private void cmdConsoleUI_BATTLEPASS_PAGE(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            ShowLevelList(player, int.Parse(arg.Args[0]));
        }

        [ConsoleCommand("UI_BATTLEPASS_INFORMATION")]
        private void cmdConsoleUI_BATTLEPASS_INFORMATION(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            ShowInformation(player);
        }

        [ConsoleCommand("UI_BATTLEPASS_TAKE")]
        private void cmdConsoleUI_BATTLEPASS_TAKE(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            var level = int.Parse(arg.Args[1]);
            var page = int.Parse(arg.Args[2]);
            switch (arg.Args[0])
            {
                case "DEFAULT":
                {
                    if (_data[player.userID].DefaultRewardID.Contains(level)) return;
                    _data[player.userID].DefaultRewardID.Add(level);
                    var settings = _config.DefaultLevelList.FirstOrDefault(p => p.LevelID == level);
                    if (settings == null)
                    {
                        player.ChatMessage($"Technical error: {level}");
                        return;
                    }

                    foreach (var check in settings.RewardList)
                    {
                        var item = ItemManager.CreateByName(check.ShortName, check.Amount, check.SkinID);
                        if (item != null)
                        {
                            if (check.isBlueprint)
                            {
                                item = ItemManager.CreateByItemID(-996920608);
                                var info = ItemManager.FindItemDefinition(check.ShortName);
                                item.blueprintTarget = info.itemid;
                            }

                            if (string.IsNullOrEmpty(check.displayName))
                                item.name = check.displayName;
                            player.GiveItem(item);
                        }

                        foreach (var cmd in check.CmdList) rust.RunServerCommand(cmd.Replace("%STEAMID%", player.UserIDString));
                    }

                    ShowLevelList(player, page);
                    break;
                }
                case "VIP":
                {
                    if (_data[player.userID].VipRewardID.Contains(level)) return;
                    _data[player.userID].VipRewardID.Add(level);
                    var settings = _config.VipLevelList.FirstOrDefault(p => p.LevelID == level);
                    if (settings == null)
                    {
                        player.ChatMessage($"Technical error: {level}");
                        return;
                    }

                    foreach (var check in settings.RewardList)
                    {
                        var item = ItemManager.CreateByName(check.ShortName, check.Amount, check.SkinID);
                        if (item != null)
                        {
                            if (check.isBlueprint)
                            {
                                item = ItemManager.CreateByItemID(-996920608);
                                var info = ItemManager.FindItemDefinition(check.ShortName);
                                item.blueprintTarget = info.itemid;
                            }
                            if (string.IsNullOrEmpty(check.displayName))
                                item.name = check.displayName;
                            player.GiveItem(item);
                        }

                        foreach (var cmd in check.CmdList) rust.RunServerCommand(cmd.Replace("%STEAMID%", player.UserIDString));
                    }

                    ShowLevelList(player, page);
                    break;
                }
            }
        }

        [ConsoleCommand("UI_BATTLEPASS_GIVEEXP")]
        private void cmdConsoleUI_BATTLEPASS_GIVEEXP(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin) return;
            try
            {
                var userID = ulong.Parse(arg.Args[0]);
                var exp = int.Parse(arg.Args[1]);
                GiveExp(userID, exp);
                PrintWarning($"Player {userID} is awarded {exp} exp");
            }
            catch
            {
                PrintError("Use UI_BATTLEPASS_GIVEEXP STEAMID Amount");
            }

        }

        private void GiveExp(BasePlayer player, int amount)
        {
            GiveExp(player.userID, amount, player);
        }


        private void GiveExp(ulong userID, int amount, BasePlayer player = null)
        {
            if (!_data.ContainsKey(userID)) return;
            amount = (int)(amount * GetMultiplier(userID));
            var data = _data[userID];
            var settings = _config.DefaultLevelList.FirstOrDefault(p => p.LevelID == data.Level + 1);
            if (settings == null) return;
            data.Score += amount;
            if (data.Score < settings.ExpForLevel) return;
            if (_config.EnableNotification && player != null)
            {
                Player.Message(player, GetMessage("MSG_NEWLEVEL", player.UserIDString, settings.LevelID));
                if (!string.IsNullOrEmpty(_config.Effect))
                    Effect.server.Run(_config.Effect, player.eyes.position);
            }

            data.Level++;
            data.Score -= settings.ExpForLevel;
            GiveExp(userID, 0);
        }


        private void RemoveExp(ulong userID, int Amount)
        {
            if (!_data.ContainsKey(userID)) return;
            _data[userID].Score -= Amount;
            _data[userID].Score = _data[userID].Score < 0 ? 0 : _data[userID].Score;
        }

        private float GetMultiplier(ulong userID)
        {
            var mult = 0f;
            foreach (var check in _config.Points.PermissionList)
                if (permission.UserHasPermission(userID.ToString(), check.Key))
                    mult = Math.Max(mult, check.Value);
            return mult;
        }

        #endregion

        #region UI

        private void ShowMainUI(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.8", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "Overlay", "Panel_6967");

            container.Add(new CuiButton
            {
                Button = { Color = "0.572549 0.282353 0.2862745 1", Close = "Panel_6967" },
                Text = { Text = GetMessage("UI_CLOSE", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "392.445 287", OffsetMax = "499.375 317" }
            }, "Panel_6967", "Button_1403");

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 1", Sprite = "assets/icons/close.png", Close = "Panel_6967" },
                Text = { Text = "   ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-47.4 -9.5", OffsetMax = "-27.4 9.5" }
            }, "Button_1403", "Button_1973");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2078432 0.2078432 0.2078432 0.8" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-426.998 215", OffsetMax = "499.372 275" }
            }, "Panel_6967", "Panel_8459");

            container.Add(new CuiButton
            {
                Button = { Color = "0.5333334 0.5333334 0.5333334 1" },
                Text = { Text = GetMessage("UI_INFO_LEVEL", player.UserIDString, _data[player.userID].Level), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-443.18 -15", OffsetMax = "-346.18 15" }
            }, "Panel_8459", "Button_1944");

            container.Add(new CuiButton
            {
                Button = { Color = "0.5333334 0.5333334 0.5333334 1", Material = "assets/content/ui/uibackgroundblur.mat" },
                Text = { Text = "   ", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-336.18 -15", OffsetMax = "285.786 15" }
            }, "Panel_8459", "SCALEmain");
            var score = _data[player.userID].Score;
            var scale = 1;
            var maxExp = score;
            var settings = _config.DefaultLevelList.FirstOrDefault(p => p.LevelID == _data[player.userID].Level + 1);
            if (settings != null)
            {
                scale = score * 100 / settings.ExpForLevel;
                maxExp = settings.ExpForLevel;
            }

            container.Add(new CuiButton
            {
                Button = { Color = "0.4235294 0.5843138 0.3568628 1" },
                Text = { Text = "   ", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = $"{scale * 0.01} 0.95" }
            }, "SCALEmain", "scaleprogress");
            container.Add(new CuiElement
            {
                Name = "Label_7031",
                Parent = "SCALEmain",
                Components =
                {
                    new CuiTextComponent { Text = $"{score}/{maxExp}", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-310.978 -15", OffsetMax = "310.982 15" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0.5333334 0.5333334 0.5333334 1", Command = "UI_BATTLEPASS_INFORMATION" },
                Text = { Text = GetMessage("UI_INFO", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "310.148 -15", OffsetMax = "454.852 15" }
            }, "Panel_8459", "Button_1944 (2)");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2078432 0.2078432 0.2078432 0.8" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-426.998 175", OffsetMax = "499.372 205" }
            }, "Panel_6967", "LEVELNAMES");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2078432 0.2078432 0.2078432 0.8" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-574 25", OffsetMax = "-437 155" }
            }, "Panel_6967", "defaultImage");

            container.Add(new CuiElement
            {
                Name = "Image_643",
                Parent = "defaultImage",
                Components =
                {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageList[_config.DefaultImage] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -50", OffsetMax = "50 50" }
                }
            });


            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2078432 0.2078432 0.2078432 0.8" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-574 -125", OffsetMax = "-437 5" }
            }, "Panel_6967", "vipimage");

            container.Add(new CuiElement
            {
                Name = "Image_643",
                Parent = "vipimage",
                Components =
                {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageList[_config.VipImage] },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-50 -50", OffsetMax = "50 50" }
                }
            });
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2078431 0.2078431 0.2078431 0.8" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-574 175", OffsetMax = "-437 275" }
            }, "Panel_6967", "Panel_8833");

            container.Add(new CuiElement
            {
                Name = "Label_2379",
                Parent = "Panel_8833",
                Components =
                {
                    new CuiTextComponent { Text = GetMessage("UI_ADVERTS", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-68.5 -50", OffsetMax = "68.5 50" }
                }
            });
            CuiHelper.DestroyUi(player, "Panel_6967");
            CuiHelper.AddUi(player, container);
            ShowLevelList(player);
        }

        private void ShowLevelList(BasePlayer player, int page = 0)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2078432 0.2078432 0.2078432 0.8" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-427 25", OffsetMax = "499.372 155" }
            }, "Panel_6967", "defaultLevels");
            var posx = -453.1;
            var width = -373.19 - posx;
            var i = 0;
            var hasPerm = permission.UserHasPermission(player.UserIDString, _config.DefaultRewardPermission);
            var data = _data[player.userID];
            foreach (var check in _config.DefaultLevelList.Skip(page * 10))
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.4333334 0.4333334 0.4333334 1", Material = "assets/content/ui/uibackgroundblur.mat" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{posx} -55", OffsetMax = $"{posx + width} 55" }
                }, "defaultLevels", "RewardPanel");
                if (check.OutlineEnable)
                    Outline(ref container, "RewardPanel", "0.3921569 0.6176471 0.3372549 1", "4");
                container.Add(new CuiElement
                {
                    Name = "Label_1226",
                    Parent = "RewardPanel",
                    Components =
                    {
                        new CuiTextComponent { Text = GetMessage("UI_LEVEL", player.UserIDString, check.LevelID), Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-40 84.287", OffsetMax = "40 115.012" }
                    }
                });
                var isTaked =  !hasPerm || data.Level < check.LevelID || data.DefaultRewardID.Contains(check.LevelID);
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.2470588 0.2470588 0.2470588 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -15", OffsetMax = "30 45" }
                }, "RewardPanel", "Panel_5825");

                if (check.RewardList.First().isBlueprint)
                {
                    container.Add(new CuiElement
                    {
                         Name = "Image_7251",
                        Parent = "Panel_5825",
                        Components =
                        {
                            new CuiRawImageComponent {  Color = isTaked ? "1 1 1 0.3" : "1 1 1 1", Png = ImageList["blueprintbase"] },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" },
                        }
                    });
                    container.Add(new CuiElement
                {
                    Name = "Image_7251sss",
                    Parent = "Image_7251",
                    Components =
                    {
                        new CuiRawImageComponent { Color = isTaked ? "1 1 1 0.8" : "1 1 1 1", Png = ImageList[check.Image] },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-20 -20", OffsetMax = "20 20" }
                    }
                });
                }
                else
                {
                    container.Add(new CuiElement
                {
                    Name = "Image_7251",
                    Parent = "Panel_5825",
                    Components =
                    {
                        new CuiRawImageComponent { Color = isTaked ? "1 1 1 0.3" : "1 1 1 1", Png = ImageList[check.Image] },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -25", OffsetMax = "25 25" }
                    }
                });
                }
                
                container.Add(new CuiButton
                {
                    Button = { Color = "0.2470588 0.2470588 0.2470588 1", Command = isTaked ? "" : $"UI_BATTLEPASS_TAKE DEFAULT {check.LevelID} {page}" },
                    Text =
                    {
                        Text = _data[player.userID].DefaultRewardID.Contains(check.LevelID) ? GetMessage("UI_NOTAKE", player.UserIDString) : GetMessage("UI_TAKE", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter,
                        Color = isTaked ? "1 1 1 0.2" : "1 1 1 1"
                    },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -44.984", OffsetMax = "30 -24.984" }
                }, "RewardPanel", "Button_336");
                var amount = check.RewardList.First().Amount;
                amount = amount > 0 ? amount : 1;
                container.Add(new CuiElement
                {
                    Name = "Label_1226ss",
                    Parent = "Image_7251",
                    Components =
                    {
                        new CuiTextComponent { Text = $"x{amount}", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.LowerRight, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0 -0.05", AnchorMax = "0.98 0.5" }
                    }
                });
                i++;
                if (i > 9) break;
                posx += width + 12;
            }

            i = 0;
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2078432 0.2078432 0.2078432 0.8" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-428.186 -125", OffsetMax = "498.186 5" }
            }, "Panel_6967", "viplevels");
            posx = -453.1;
            hasPerm = permission.UserHasPermission(player.UserIDString, _config.VipRewardPermission);
            foreach (var check in _config.VipLevelList.Skip(page * 10))
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.3529412 0.3529412 0.3529412 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{posx} -55", OffsetMax = $"{posx + width} 55" }
                }, "viplevels", "RewardPanel");
                if (check.OutlineEnable)
                    Outline(ref container, "RewardPanel", "0.3921569 0.6176471 0.3372549 1", "4");

                var isTaked = !hasPerm || data.Level < check.LevelID || data.VipRewardID.Contains(check.LevelID);
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.2470588 0.2470588 0.2470588 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -15", OffsetMax = "30 45" }
                }, "RewardPanel", "Panel_5825");
                if (check.RewardList.First().isBlueprint)
                {
                    container.Add(new CuiElement
                    {
                        Name = "Image_7251",
                        Parent = "Panel_5825",
                        Components =
                        {
                            new CuiRawImageComponent {  Color = isTaked ? "1 1 1 0.3" : "1 1 1 1", Png = ImageList["blueprintbase"] },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -30", OffsetMax = "30 30" },
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Name = "Image_7251sss",
                        Parent = "Image_7251",
                        Components =
                        {
                            new CuiRawImageComponent { Color = isTaked ? "1 1 1 0.8" : "1 1 1 1", Png = ImageList[check.Image] },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-20 -20", OffsetMax = "20 20" }
                        }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Name = "Image_7251",
                        Parent = "Panel_5825",
                        Components =
                        {
                            new CuiRawImageComponent { Color = isTaked ? "1 1 1 0.3" : "1 1 1 1", Png = ImageList[check.Image] },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -25", OffsetMax = "25 25" }
                        }
                    });
                }
                container.Add(new CuiButton
                {
                    Button = { Color = "0.2470588 0.2470588 0.2470588 1", Command = isTaked ? "" : $"UI_BATTLEPASS_TAKE VIP {check.LevelID} {page}" },
                    Text =
                    {
                        Text = _data[player.userID].VipRewardID.Contains(check.LevelID) ? GetMessage("UI_NOTAKE", player.UserIDString) : GetMessage("UI_TAKE", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.MiddleCenter,
                        Color = isTaked ? "1 1 1 0.2" : "1 1 1 1"
                    },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -44.984", OffsetMax = "30 -24.984" }
                }, "RewardPanel", "Button_336");
                 var amount = check.RewardList.First().Amount;
                amount = amount > 0 ? amount : 1;
                container.Add(new CuiElement
                {
                    Name = "Label_1226ss",
                    Parent = "Image_7251",
                    Components =
                    {
                        new CuiTextComponent { Text = $"x{amount}", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.LowerRight, Color = "1 1 1 1" },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent { AnchorMin = "0 -0.05", AnchorMax = "0.98 0.5" }
                    }
                });
                i++;
                if (i > 9) break;
                posx += width + 12;
            }


            if (_config.DefaultLevelList.Count / 10f > page + 1)
                container.Add(new CuiButton
                {
                    Button = { Color = "0.2078431 0.2078431 0.2078431 1", Command = $"UI_BATTLEPASS_PAGE {page + 1}" },
                    Text = { Text = "▶", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "1.252 -164.479", OffsetMax = "29.748 -134.722" }
                }, "Panel_6967", "Button_6917");

            if (page > 0)
                container.Add(new CuiButton
                {
                    Button = { Color = "0.2078431 0.2078431 0.2078431 1", Command = $"UI_BATTLEPASS_PAGE {page - 1}" },
                    Text = { Text = "◀ ", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-29.748 -164.479", OffsetMax = "-1.252 -134.722" }
                }, "Panel_6967", "Button_6917 (1)");
            CuiHelper.DestroyUi(player, "viplevels");
            CuiHelper.DestroyUi(player, "Button_6917 (1)");
            CuiHelper.DestroyUi(player, "Button_6917");
            CuiHelper.DestroyUi(player, "defaultLevels");
            CuiHelper.AddUi(player, container);
        }

        private void ShowInformation(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.2078431 0.2078431 0.2078431 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-427.171 -123.867", OffsetMax = "499.372 273.841" }
            }, "Overlay", "Panel_8480");

            container.Add(new CuiElement
            {
                Name = "Label_6187",
                Parent = "Panel_8480",
                Components =
                {
                    new CuiTextComponent { Text = GetMessage("UI_INFO", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-95.383 153.704", OffsetMax = "95.383 198.856" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "Label_8187",
                Parent = "Panel_8480",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = GetMessage("UI_INFO_TEXT", player.UserIDString),
                        Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1"
                    },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-417.377 -179.644", OffsetMax = "479.372 146.029" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0.572549 0.282353 0.2862745 1", Close = "Panel_8480" },
                Text = { Text = GetMessage("UI_CLOSE", player.UserIDString), Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "342.54 157.6", OffsetMax = "449.372 187.6" }
            }, "Panel_8480", "Button_1403 (1)");


            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 1", Sprite = "assets/icons/close.png", Close = "Panel_8480" },
                Text = { Text = "   ", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-44.4 -9.5", OffsetMax = "-27.4 9.5" }
            }, "Button_1403 (1)", "Button_1973");

            CuiHelper.DestroyUi(player, "Panel_8480");
            CuiHelper.AddUi(player, container);
        }

        private void Outline(ref CuiElementContainer container, string parent, string color = "1 1 1 1",
            string size = "2.5")
        {
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = $"0 {size}" },
                Image = { Color = color }
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 -{size}", OffsetMax = "0 0" },
                Image = { Color = color }
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform =
                    { AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = $"0 {size}", OffsetMax = $"{size} -{size}" },
                Image = { Color = color }
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform =
                    { AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = $"-{size} {size}", OffsetMax = $"0 -{size}" },
                Image = { Color = color }
            }, parent);
        }

        #region Lang

        

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_TAKE"] = "Take",
                ["MSG_NEWLEVEL"] = "You have raised the level of your battle pass to {0}",
                ["UI_ADVERTS"] = "To access VIP rewards, you need to purchase access on the website discord.com ",
                ["UI_LEVEL"] = "LVL {0}",
                ["UI_NOTAKE"] = "<color=#648456>Received</color>",
                ["UI_INFO_LEVEL"] = "LVL {0}",
                ["UI_CLOSE"] = "CLOSE   ",
                ["UI_INFO"] = "INFORMATION",
                ["UI_INFO_TEXT"] = "" +
                                   "●   Entering the battle pass, you will see 2 chains of 30 levels. A simple pass is available to all players.\n" +
                                   "●   The Epic pass can be purchased in the server store: <color=#BD4C35>discord.com</color>\n" +
                                   "●   \n" +
                                   "●   How to pass a battle pass? Everything is simple, below I will give the number of points and what to get them for:\n" +
                                   "●   Number of points for killing animals: 1          ●   Number of points for destroying a helicopter: 10\n" +
                                   "●   Number of points for killing NPCs: 1             ●   The number of points for destroying the tank: 10\n" +
                                   "●   Number of points deducted for death: 5            \n" +
                                   "●   Also, points are awarded for resource extraction\n" +
                                   "●   Extraction: Wood:1 point.                        ●  Mining: Stone: 1 point.\n" +
                                   "●   Mining: Metal Ore: 1 point                       ● Mining: Sulfur ore: 1 point\n" +
                                   "●   In the server store: <color=#BD4C35>discord.com </color> there is a product of 2x glasses. Buying it, all your points are multiplied by x2."
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_TAKE"] = "Получить",
                ["UI_ADVERTS"] = "Для доступа к VIP наградам вам нужно приобрести доступ на сайте discord.com",
                ["UI_NOTAKE"] = "<color=#648456>Получено</color>",
                ["MSG_NEWLEVEL"] = "Вы повысили уровень вашего боевого пропуска до {0}",
                ["UI_LEVEL"] = "LVL {0}",
                ["UI_INFO_LEVEL"] = "LVL {0}",
                ["UI_CLOSE"] = "ЗАКРЫТЬ   ",
                ["UI_INFO"] = "ИНФОРМАЦИЯ",
                ["UI_INFO_TEXT"] = "" +
                                   "●   Войдя в боевой пропуск, вы увидете 2 цепочки по 30 уровней. Простой пропуск доступен всем игрокам.\n" +
                                   "●   Эпический пропуск можно купить в магазине сервера: <color=#BD4C35>discord.com</color>\n" +
                                   "●   \n" +
                                   "●   Как проходить боевой пропуск? Все просто, ниже приведу количество очков и за что их получить:\n" +
                                   "●   Количество очков за убийство животных: 1               ●   Количество очков за уничтожение вертолета:  10\n" +
                                   "●   Количество очков за убийство НПС: 1                    ●   Количество очков за уничтожение танка:         10\n" +
                                   "●   Количество отнимаемых очков за смерть: 5              \n" +
                                   "●   Также очки начисляются за добычу ресурсов\n" +
                                   "●   Добыча: Дерева:1 очко.                                 ●   Добыча: Камня: 1 очко.\n" +
                                   "●   Добыча: Металлической руды: 1 очко                     ●   Добыча: Серной руды:              1 очко\n" +
                                   "●   В магазине сервера: <color=#BD4C35>discord.com</color> есть товар 2х очки. Покупая его, все ваши очки умножаются на х2."
            }, this, "ru");
        }

        private string GetMessage(string langKey, string steamID)
        {
            return lang.GetMessage(langKey, this, steamID);
        }

        private void SendPlayerMessage(BasePlayer player, string langKey,params object[] args)
        {
            Player.Message(player,GetMessage(langKey, player.UserIDString,args));
        }
        private string GetMessage(string langKey, string steamID, params object[] args)
        {
            return args.Length == 0
                ? GetMessage(langKey, steamID)
                : string.Format(GetMessage(langKey, steamID), args);
        }
        #endregion

        #endregion
    }
}
