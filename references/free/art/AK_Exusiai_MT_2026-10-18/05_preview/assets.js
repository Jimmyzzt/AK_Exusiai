var MT_ASSETS = {
  "size": [
    1920,
    1080
  ],
  "backgrounds": [
    {
      "label": "MT-1 圣秩",
      "path": "02_created/backgrounds/MT-01_sanctuary.png",
      "source": "01_originals/maps/MT-1/map_preview.png"
    },
    {
      "label": "MT-2 祈祷",
      "path": "02_created/backgrounds/MT-02_prayer_lanes.png",
      "source": "01_originals/maps/MT-2/map_preview.png"
    },
    {
      "label": "MT-3 礼拜",
      "path": "02_created/backgrounds/MT-03_worship_court.png",
      "source": "01_originals/maps/MT-3/map_preview.png"
    },
    {
      "label": "MT-4 求道",
      "path": "02_created/backgrounds/MT-04_seeking_aisle.png",
      "source": "01_originals/maps/MT-4/map_preview.png"
    },
    {
      "label": "MT-5 共融",
      "path": "02_created/backgrounds/MT-05_communion_gallery.png",
      "source": "01_originals/maps/MT-5/map_preview.png"
    },
    {
      "label": "MT-6 告解",
      "path": "02_created/backgrounds/MT-06_confession_crossing.png",
      "source": "01_originals/maps/MT-6/map_preview.png"
    },
    {
      "label": "MT-7 丧礼",
      "path": "02_created/backgrounds/MT-07_funeral_choir.png",
      "source": "01_originals/maps/MT-7/map_preview.png"
    },
    {
      "label": "MT-8 朝圣",
      "path": "02_created/backgrounds/MT-08_pilgrimage_terraces.png",
      "source": "01_originals/maps/MT-8/map_preview.png"
    },
    {
      "label": "MT-9 解经",
      "path": "02_created/backgrounds/MT-09_exegesis_chancel.png",
      "source": "01_originals/maps/MT-9/map_preview.png"
    }
  ],
  "families": {
    "MT-10": [
      {
        "label": "梦中",
        "path": "02_created/MT-10/00_dream.png",
        "layer": null,
        "extra": false
      },
      {
        "label": "梦醒",
        "path": "02_created/MT-10/01_awake.png",
        "layer": "02_created/MT-10/layers/reveal_1.png",
        "extra": false
      }
    ],
    "MT-S-5": [
      {
        "label": "梦中",
        "path": "02_created/MT-S-5/00_dream.png",
        "layer": null,
        "extra": false
      },
      {
        "label": "揭示①",
        "path": "02_created/MT-S-5/01_reveal_1.png",
        "layer": "02_created/MT-S-5/layers/reveal_1.png",
        "extra": false
      },
      {
        "label": "揭示②",
        "path": "02_created/MT-S-5/02_reveal_2.png",
        "layer": "02_created/MT-S-5/layers/reveal_2.png",
        "extra": false
      },
      {
        "label": "揭示③",
        "path": "02_created/MT-S-5/03_reveal_3.png",
        "layer": "02_created/MT-S-5/layers/reveal_3.png",
        "extra": false
      },
      {
        "label": "揭示④",
        "path": "02_created/MT-S-5/04_reveal_4_conditional.png",
        "layer": "02_created/MT-S-5/layers/reveal_4.png",
        "extra": true
      }
    ]
  },
  "boss": {
    "ground": "02_created/boss/saint_ground.png",
    "air": "02_created/boss/saint_airborne_concept.png"
  },
  "boss_bounds": {
    "ground": [
      152,
      194,
      1394,
      1395
    ],
    "air": [
      141,
      166,
      1395,
      1395
    ]
  },
  "player": "05_preview/exusiai_scale_reference.png",
  "player_bounds": [
    293,
    200,
    757,
    813
  ],
  "enemies": [
    {
      "id": "enemy_10083_hlbird_2",
      "name": "“萨科塔展翅”",
      "stages": [
        "MT-S-5"
      ],
      "scope": "MT-S-5_supplement",
      "action_count": 4,
      "actions": [
        {
          "enemy_id": "enemy_10083_hlbird_2",
          "enemy_name": "“萨科塔展翅”",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10083_hlbird_2",
          "enemy_name": "“萨科塔展翅”",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10083_hlbird_2",
          "enemy_name": "“萨科塔展翅”",
          "animation": "Idle",
          "duration_seconds": 0.66667,
          "approx_frames_30fps": 20,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10083_hlbird_2",
          "enemy_name": "“萨科塔展翅”",
          "animation": "Move",
          "duration_seconds": 0.66667,
          "approx_frames_30fps": 20,
          "events": [],
          "scope": "MT-S-5_supplement"
        }
      ],
      "folder": "01_originals/enemies/enemy_10083_hlbird_2_“萨科塔展翅”",
      "source": "https://prts.wiki/w/“萨科塔展翅”"
    },
    {
      "id": "enemy_10083_hlbird",
      "name": "“萨科塔之翼”",
      "stages": [
        "MT-5",
        "MT-6",
        "MT-7",
        "MT-8",
        "MT-10"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 4,
      "actions": [
        {
          "enemy_id": "enemy_10083_hlbird",
          "enemy_name": "“萨科塔之翼”",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10083_hlbird",
          "enemy_name": "“萨科塔之翼”",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10083_hlbird",
          "enemy_name": "“萨科塔之翼”",
          "animation": "Idle",
          "duration_seconds": 0.66667,
          "approx_frames_30fps": 20,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10083_hlbird",
          "enemy_name": "“萨科塔之翼”",
          "animation": "Move",
          "duration_seconds": 0.66667,
          "approx_frames_30fps": 20,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_10083_hlbird_“萨科塔之翼”",
      "source": "https://prts.wiki/w/“萨科塔之翼”"
    },
    {
      "id": "enemy_10084_hlegle_2",
      "name": "“萨科塔见证”",
      "stages": [
        "MT-S-5"
      ],
      "scope": "MT-S-5_supplement",
      "action_count": 5,
      "actions": [
        {
          "enemy_id": "enemy_10084_hlegle_2",
          "enemy_name": "“萨科塔见证”",
          "animation": "Attack",
          "duration_seconds": 1.5,
          "approx_frames_30fps": 45,
          "events": [
            {
              "time": 0.76667,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10084_hlegle_2",
          "enemy_name": "“萨科塔见证”",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10084_hlegle_2",
          "enemy_name": "“萨科塔见证”",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10084_hlegle_2",
          "enemy_name": "“萨科塔见证”",
          "animation": "Idle",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10084_hlegle_2",
          "enemy_name": "“萨科塔见证”",
          "animation": "Move",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [],
          "scope": "MT-S-5_supplement"
        }
      ],
      "folder": "01_originals/enemies/enemy_10084_hlegle_2_“萨科塔见证”",
      "source": "https://prts.wiki/w/“萨科塔见证”"
    },
    {
      "id": "enemy_10084_hlegle",
      "name": "“萨科塔之眼”",
      "stages": [
        "MT-5",
        "MT-6",
        "MT-7",
        "MT-8",
        "MT-10"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 5,
      "actions": [
        {
          "enemy_id": "enemy_10084_hlegle",
          "enemy_name": "“萨科塔之眼”",
          "animation": "Attack",
          "duration_seconds": 1.5,
          "approx_frames_30fps": 45,
          "events": [
            {
              "time": 0.76667,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10084_hlegle",
          "enemy_name": "“萨科塔之眼”",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10084_hlegle",
          "enemy_name": "“萨科塔之眼”",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10084_hlegle",
          "enemy_name": "“萨科塔之眼”",
          "animation": "Idle",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10084_hlegle",
          "enemy_name": "“萨科塔之眼”",
          "animation": "Move",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_10084_hlegle_“萨科塔之眼”",
      "source": "https://prts.wiki/w/“萨科塔之眼”"
    },
    {
      "id": "enemy_10085_hllevi_2",
      "name": "“萨科塔昂首”",
      "stages": [
        "MT-S-5"
      ],
      "scope": "MT-S-5_supplement",
      "action_count": 5,
      "actions": [
        {
          "enemy_id": "enemy_10085_hllevi_2",
          "enemy_name": "“萨科塔昂首”",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10085_hllevi_2",
          "enemy_name": "“萨科塔昂首”",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10085_hllevi_2",
          "enemy_name": "“萨科塔昂首”",
          "animation": "Idle",
          "duration_seconds": 2,
          "approx_frames_30fps": 60,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10085_hllevi_2",
          "enemy_name": "“萨科塔昂首”",
          "animation": "Move",
          "duration_seconds": 2,
          "approx_frames_30fps": 60,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10085_hllevi_2",
          "enemy_name": "“萨科塔昂首”",
          "animation": "Skill",
          "duration_seconds": 2.17,
          "approx_frames_30fps": 65,
          "events": [
            {
              "time": 1,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-S-5_supplement"
        }
      ],
      "folder": "01_originals/enemies/enemy_10085_hllevi_2_“萨科塔昂首”",
      "source": "https://prts.wiki/w/“萨科塔昂首”"
    },
    {
      "id": "enemy_10085_hllevi",
      "name": "“萨科塔之鳍”",
      "stages": [
        "MT-7",
        "MT-8"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 5,
      "actions": [
        {
          "enemy_id": "enemy_10085_hllevi",
          "enemy_name": "“萨科塔之鳍”",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10085_hllevi",
          "enemy_name": "“萨科塔之鳍”",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10085_hllevi",
          "enemy_name": "“萨科塔之鳍”",
          "animation": "Idle",
          "duration_seconds": 2,
          "approx_frames_30fps": 60,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10085_hllevi",
          "enemy_name": "“萨科塔之鳍”",
          "animation": "Move",
          "duration_seconds": 2,
          "approx_frames_30fps": 60,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10085_hllevi",
          "enemy_name": "“萨科塔之鳍”",
          "animation": "Skill",
          "duration_seconds": 2.17,
          "approx_frames_30fps": 65,
          "events": [
            {
              "time": 1,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_10085_hllevi_“萨科塔之鳍”",
      "source": "https://prts.wiki/w/“萨科塔之鳍”"
    },
    {
      "id": "enemy_10086_hltigr",
      "name": "“萨科塔之爪”",
      "stages": [
        "MT-6",
        "MT-8",
        "MT-10"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 5,
      "actions": [
        {
          "enemy_id": "enemy_10086_hltigr",
          "enemy_name": "“萨科塔之爪”",
          "animation": "Attack",
          "duration_seconds": 1.13,
          "approx_frames_30fps": 34,
          "events": [
            {
              "time": 0.5,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10086_hltigr",
          "enemy_name": "“萨科塔之爪”",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10086_hltigr",
          "enemy_name": "“萨科塔之爪”",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10086_hltigr",
          "enemy_name": "“萨科塔之爪”",
          "animation": "Idle",
          "duration_seconds": 1.5,
          "approx_frames_30fps": 45,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10086_hltigr",
          "enemy_name": "“萨科塔之爪”",
          "animation": "Move",
          "duration_seconds": 1.07,
          "approx_frames_30fps": 32,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_10086_hltigr_“萨科塔之爪”",
      "source": "https://prts.wiki/w/“萨科塔之爪”"
    },
    {
      "id": "enemy_10087_hlchgr_2",
      "name": "精锐圣堂剑士",
      "stages": [
        "MT-S-5"
      ],
      "scope": "MT-S-5_supplement",
      "action_count": 6,
      "actions": [
        {
          "enemy_id": "enemy_10087_hlchgr_2",
          "enemy_name": "精锐圣堂剑士",
          "animation": "Attack",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [
            {
              "time": 0.43333,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10087_hlchgr_2",
          "enemy_name": "精锐圣堂剑士",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10087_hlchgr_2",
          "enemy_name": "精锐圣堂剑士",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10087_hlchgr_2",
          "enemy_name": "精锐圣堂剑士",
          "animation": "Idle",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10087_hlchgr_2",
          "enemy_name": "精锐圣堂剑士",
          "animation": "Move",
          "duration_seconds": 0.66667,
          "approx_frames_30fps": 20,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10087_hlchgr_2",
          "enemy_name": "精锐圣堂剑士",
          "animation": "Skill",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [
            {
              "time": 0.33333,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-S-5_supplement"
        }
      ],
      "folder": "01_originals/enemies/enemy_10087_hlchgr_2_精锐圣堂剑士",
      "source": "https://prts.wiki/w/精锐圣堂剑士"
    },
    {
      "id": "enemy_10087_hlchgr",
      "name": "圣堂剑士",
      "stages": [
        "MT-2",
        "MT-7",
        "MT-8",
        "MT-9",
        "MT-10"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 6,
      "actions": [
        {
          "enemy_id": "enemy_10087_hlchgr",
          "enemy_name": "圣堂剑士",
          "animation": "Attack",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [
            {
              "time": 0.43333,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10087_hlchgr",
          "enemy_name": "圣堂剑士",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10087_hlchgr",
          "enemy_name": "圣堂剑士",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10087_hlchgr",
          "enemy_name": "圣堂剑士",
          "animation": "Idle",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10087_hlchgr",
          "enemy_name": "圣堂剑士",
          "animation": "Move",
          "duration_seconds": 0.66667,
          "approx_frames_30fps": 20,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10087_hlchgr",
          "enemy_name": "圣堂剑士",
          "animation": "Skill",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [
            {
              "time": 0.33333,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_10087_hlchgr_圣堂剑士",
      "source": "https://prts.wiki/w/圣堂剑士"
    },
    {
      "id": "enemy_10088_hlsnip_2",
      "name": "精锐圣堂射手",
      "stages": [
        "MT-S-5"
      ],
      "scope": "MT-S-5_supplement",
      "action_count": 8,
      "actions": [
        {
          "enemy_id": "enemy_10088_hlsnip_2",
          "enemy_name": "精锐圣堂射手",
          "animation": "Attack",
          "duration_seconds": 1.73,
          "approx_frames_30fps": 52,
          "events": [
            {
              "time": 0.63333,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10088_hlsnip_2",
          "enemy_name": "精锐圣堂射手",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10088_hlsnip_2",
          "enemy_name": "精锐圣堂射手",
          "animation": "Die",
          "duration_seconds": 1.13,
          "approx_frames_30fps": 34,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10088_hlsnip_2",
          "enemy_name": "精锐圣堂射手",
          "animation": "Idle",
          "duration_seconds": 2.67,
          "approx_frames_30fps": 80,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10088_hlsnip_2",
          "enemy_name": "精锐圣堂射手",
          "animation": "Move",
          "duration_seconds": 2.67,
          "approx_frames_30fps": 80,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10088_hlsnip_2",
          "enemy_name": "精锐圣堂射手",
          "animation": "Skill_End",
          "duration_seconds": 1.1,
          "approx_frames_30fps": 33,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10088_hlsnip_2",
          "enemy_name": "精锐圣堂射手",
          "animation": "Skill_Loop",
          "duration_seconds": 3.33,
          "approx_frames_30fps": 100,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10088_hlsnip_2",
          "enemy_name": "精锐圣堂射手",
          "animation": "Skill_Start",
          "duration_seconds": 0.53333,
          "approx_frames_30fps": 16,
          "events": [],
          "scope": "MT-S-5_supplement"
        }
      ],
      "folder": "01_originals/enemies/enemy_10088_hlsnip_2_精锐圣堂射手",
      "source": "https://prts.wiki/w/精锐圣堂射手"
    },
    {
      "id": "enemy_10088_hlsnip",
      "name": "圣堂射手",
      "stages": [
        "MT-3"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 8,
      "actions": [
        {
          "enemy_id": "enemy_10088_hlsnip",
          "enemy_name": "圣堂射手",
          "animation": "Attack",
          "duration_seconds": 1.73,
          "approx_frames_30fps": 52,
          "events": [
            {
              "time": 0.63333,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10088_hlsnip",
          "enemy_name": "圣堂射手",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10088_hlsnip",
          "enemy_name": "圣堂射手",
          "animation": "Die",
          "duration_seconds": 1.13,
          "approx_frames_30fps": 34,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10088_hlsnip",
          "enemy_name": "圣堂射手",
          "animation": "Idle",
          "duration_seconds": 2.67,
          "approx_frames_30fps": 80,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10088_hlsnip",
          "enemy_name": "圣堂射手",
          "animation": "Move",
          "duration_seconds": 2.67,
          "approx_frames_30fps": 80,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10088_hlsnip",
          "enemy_name": "圣堂射手",
          "animation": "Skill_End",
          "duration_seconds": 1.1,
          "approx_frames_30fps": 33,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10088_hlsnip",
          "enemy_name": "圣堂射手",
          "animation": "Skill_Loop",
          "duration_seconds": 3.33,
          "approx_frames_30fps": 100,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10088_hlsnip",
          "enemy_name": "圣堂射手",
          "animation": "Skill_Start",
          "duration_seconds": 0.53333,
          "approx_frames_30fps": 16,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_10088_hlsnip_圣堂射手",
      "source": "https://prts.wiki/w/圣堂射手"
    },
    {
      "id": "enemy_10089_hlsprt_2",
      "name": "资深圣堂保育员",
      "stages": [
        "MT-S-5"
      ],
      "scope": "MT-S-5_supplement",
      "action_count": 6,
      "actions": [
        {
          "enemy_id": "enemy_10089_hlsprt_2",
          "enemy_name": "资深圣堂保育员",
          "animation": "Attack",
          "duration_seconds": 1.83,
          "approx_frames_30fps": 55,
          "events": [
            {
              "time": 0.66667,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10089_hlsprt_2",
          "enemy_name": "资深圣堂保育员",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10089_hlsprt_2",
          "enemy_name": "资深圣堂保育员",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10089_hlsprt_2",
          "enemy_name": "资深圣堂保育员",
          "animation": "Idle",
          "duration_seconds": 2,
          "approx_frames_30fps": 60,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10089_hlsprt_2",
          "enemy_name": "资深圣堂保育员",
          "animation": "Move",
          "duration_seconds": 1.07,
          "approx_frames_30fps": 32,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10089_hlsprt_2",
          "enemy_name": "资深圣堂保育员",
          "animation": "Skill",
          "duration_seconds": 1.67,
          "approx_frames_30fps": 50,
          "events": [
            {
              "time": 0.83333,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-S-5_supplement"
        }
      ],
      "folder": "01_originals/enemies/enemy_10089_hlsprt_2_资深圣堂保育员",
      "source": "https://prts.wiki/w/资深圣堂保育员"
    },
    {
      "id": "enemy_10089_hlsprt",
      "name": "圣堂保育员",
      "stages": [
        "MT-4",
        "MT-10"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 6,
      "actions": [
        {
          "enemy_id": "enemy_10089_hlsprt",
          "enemy_name": "圣堂保育员",
          "animation": "Attack",
          "duration_seconds": 1.83,
          "approx_frames_30fps": 55,
          "events": [
            {
              "time": 0.66667,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10089_hlsprt",
          "enemy_name": "圣堂保育员",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10089_hlsprt",
          "enemy_name": "圣堂保育员",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10089_hlsprt",
          "enemy_name": "圣堂保育员",
          "animation": "Idle",
          "duration_seconds": 2,
          "approx_frames_30fps": 60,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10089_hlsprt",
          "enemy_name": "圣堂保育员",
          "animation": "Move",
          "duration_seconds": 1.07,
          "approx_frames_30fps": 32,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10089_hlsprt",
          "enemy_name": "圣堂保育员",
          "animation": "Skill",
          "duration_seconds": 1.67,
          "approx_frames_30fps": 50,
          "events": [
            {
              "time": 0.83333,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_10089_hlsprt_圣堂保育员",
      "source": "https://prts.wiki/w/圣堂保育员"
    },
    {
      "id": "enemy_10090_hlgrd",
      "name": "圣堂布道员",
      "stages": [
        "MT-9"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 8,
      "actions": [
        {
          "enemy_id": "enemy_10090_hlgrd",
          "enemy_name": "圣堂布道员",
          "animation": "Attack",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [
            {
              "time": 0.43333,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10090_hlgrd",
          "enemy_name": "圣堂布道员",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10090_hlgrd",
          "enemy_name": "圣堂布道员",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10090_hlgrd",
          "enemy_name": "圣堂布道员",
          "animation": "Idle",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10090_hlgrd",
          "enemy_name": "圣堂布道员",
          "animation": "Move",
          "duration_seconds": 1.2,
          "approx_frames_30fps": 36,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10090_hlgrd",
          "enemy_name": "圣堂布道员",
          "animation": "Skill_Begin",
          "duration_seconds": 1.17,
          "approx_frames_30fps": 35,
          "events": [
            {
              "time": 0.6,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10090_hlgrd",
          "enemy_name": "圣堂布道员",
          "animation": "Skill_End",
          "duration_seconds": 0.13333,
          "approx_frames_30fps": 4,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10090_hlgrd",
          "enemy_name": "圣堂布道员",
          "animation": "Skill_Loop",
          "duration_seconds": 0.66667,
          "approx_frames_30fps": 20,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_10090_hlgrd_圣堂布道员",
      "source": "https://prts.wiki/w/圣堂布道员"
    },
    {
      "id": "enemy_10091_hlsttu_2",
      "name": "圣徒祈祷像",
      "stages": [
        "MT-S-5"
      ],
      "scope": "MT-S-5_supplement",
      "action_count": 4,
      "actions": [
        {
          "enemy_id": "enemy_10091_hlsttu_2",
          "enemy_name": "圣徒祈祷像",
          "animation": "Attack",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [
            {
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10091_hlsttu_2",
          "enemy_name": "圣徒祈祷像",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10091_hlsttu_2",
          "enemy_name": "圣徒祈祷像",
          "animation": "Die",
          "duration_seconds": 1.2,
          "approx_frames_30fps": 36,
          "events": [],
          "scope": "MT-S-5_supplement"
        },
        {
          "enemy_id": "enemy_10091_hlsttu_2",
          "enemy_name": "圣徒祈祷像",
          "animation": "Idle",
          "duration_seconds": 2.67,
          "approx_frames_30fps": 80,
          "events": [],
          "scope": "MT-S-5_supplement"
        }
      ],
      "folder": "01_originals/enemies/enemy_10091_hlsttu_2_圣徒祈祷像",
      "source": "https://prts.wiki/w/圣徒祈祷像"
    },
    {
      "id": "enemy_10091_hlsttu_3",
      "name": "“牧首”",
      "stages": [
        "MT-8"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 4,
      "actions": [
        {
          "enemy_id": "enemy_10091_hlsttu_3",
          "enemy_name": "“牧首”",
          "animation": "Attack",
          "duration_seconds": 1.6,
          "approx_frames_30fps": 48,
          "events": [
            {
              "time": 0.83333,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10091_hlsttu_3",
          "enemy_name": "“牧首”",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10091_hlsttu_3",
          "enemy_name": "“牧首”",
          "animation": "Die",
          "duration_seconds": 1.67,
          "approx_frames_30fps": 50,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10091_hlsttu_3",
          "enemy_name": "“牧首”",
          "animation": "Idle",
          "duration_seconds": 2.67,
          "approx_frames_30fps": 80,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_10091_hlsttu_3_“牧首”",
      "source": "https://prts.wiki/w/“牧首”"
    },
    {
      "id": "enemy_10091_hlsttu",
      "name": "圣徒像",
      "stages": [
        "MT-10"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 4,
      "actions": [
        {
          "enemy_id": "enemy_10091_hlsttu",
          "enemy_name": "圣徒像",
          "animation": "Attack",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10091_hlsttu",
          "enemy_name": "圣徒像",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10091_hlsttu",
          "enemy_name": "圣徒像",
          "animation": "Die",
          "duration_seconds": 1.2,
          "approx_frames_30fps": 36,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_10091_hlsttu",
          "enemy_name": "圣徒像",
          "animation": "Idle",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_10091_hlsttu_圣徒像",
      "source": "https://prts.wiki/w/圣徒像"
    },
    {
      "id": "enemy_1140_merhd",
      "name": "战术猎犬",
      "stages": [
        "MT-1"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 5,
      "actions": [
        {
          "enemy_id": "enemy_1140_merhd",
          "enemy_name": "战术猎犬",
          "animation": "Attack",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [
            {
              "time": 0.4,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1140_merhd",
          "enemy_name": "战术猎犬",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1140_merhd",
          "enemy_name": "战术猎犬",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1140_merhd",
          "enemy_name": "战术猎犬",
          "animation": "Idle",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1140_merhd",
          "enemy_name": "战术猎犬",
          "animation": "Move",
          "duration_seconds": 0.53333,
          "approx_frames_30fps": 16,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_1140_merhd_战术猎犬",
      "source": "https://prts.wiki/w/战术猎犬"
    },
    {
      "id": "enemy_1212_mtrfol_2",
      "name": "寻路者精锐战士",
      "stages": [
        "MT-2"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 5,
      "actions": [
        {
          "enemy_id": "enemy_1212_mtrfol_2",
          "enemy_name": "寻路者精锐战士",
          "animation": "Attack",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [
            {
              "time": 0.4,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1212_mtrfol_2",
          "enemy_name": "寻路者精锐战士",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1212_mtrfol_2",
          "enemy_name": "寻路者精锐战士",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1212_mtrfol_2",
          "enemy_name": "寻路者精锐战士",
          "animation": "Idle",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1212_mtrfol_2",
          "enemy_name": "寻路者精锐战士",
          "animation": "Move",
          "duration_seconds": 0.83333,
          "approx_frames_30fps": 25,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_1212_mtrfol_2_寻路者精锐战士",
      "source": "https://prts.wiki/w/寻路者精锐战士"
    },
    {
      "id": "enemy_1212_mtrfol",
      "name": "寻路者战士",
      "stages": [
        "MT-1",
        "MT-2",
        "MT-3",
        "MT-4",
        "MT-6",
        "MT-10"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 5,
      "actions": [
        {
          "enemy_id": "enemy_1212_mtrfol",
          "enemy_name": "寻路者战士",
          "animation": "Attack",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [
            {
              "time": 0.4,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1212_mtrfol",
          "enemy_name": "寻路者战士",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1212_mtrfol",
          "enemy_name": "寻路者战士",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1212_mtrfol",
          "enemy_name": "寻路者战士",
          "animation": "Idle",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1212_mtrfol",
          "enemy_name": "寻路者战士",
          "animation": "Move",
          "duration_seconds": 0.83333,
          "approx_frames_30fps": 25,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_1212_mtrfol_寻路者战士",
      "source": "https://prts.wiki/w/寻路者战士"
    },
    {
      "id": "enemy_1213_mtrsnp",
      "name": "寻路者狙击手",
      "stages": [
        "MT-1",
        "MT-3"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 6,
      "actions": [
        {
          "enemy_id": "enemy_1213_mtrsnp",
          "enemy_name": "寻路者狙击手",
          "animation": "Attack",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [
            {
              "time": 0.5,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1213_mtrsnp",
          "enemy_name": "寻路者狙击手",
          "animation": "Attack_2",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [
            {
              "time": 0.53333,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1213_mtrsnp",
          "enemy_name": "寻路者狙击手",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1213_mtrsnp",
          "enemy_name": "寻路者狙击手",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1213_mtrsnp",
          "enemy_name": "寻路者狙击手",
          "animation": "Idle",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1213_mtrsnp",
          "enemy_name": "寻路者狙击手",
          "animation": "Move",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_1213_mtrsnp_寻路者狙击手",
      "source": "https://prts.wiki/w/寻路者狙击手"
    },
    {
      "id": "enemy_1214_mtrmgc",
      "name": "寻路者术师",
      "stages": [
        "MT-4"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 5,
      "actions": [
        {
          "enemy_id": "enemy_1214_mtrmgc",
          "enemy_name": "寻路者术师",
          "animation": "Attack",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [
            {
              "time": 0.63333,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1214_mtrmgc",
          "enemy_name": "寻路者术师",
          "animation": "Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1214_mtrmgc",
          "enemy_name": "寻路者术师",
          "animation": "Die",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1214_mtrmgc",
          "enemy_name": "寻路者术师",
          "animation": "Idle",
          "duration_seconds": 2,
          "approx_frames_30fps": 60,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1214_mtrmgc",
          "enemy_name": "寻路者术师",
          "animation": "Move",
          "duration_seconds": 1.07,
          "approx_frames_30fps": 32,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_1214_mtrmgc_寻路者术师",
      "source": "https://prts.wiki/w/寻路者术师"
    },
    {
      "id": "enemy_1567_pope",
      "name": "“圣徒”",
      "stages": [
        "MT-10",
        "MT-S-5"
      ],
      "scope": "MT-1_to_MT-10",
      "action_count": 31,
      "actions": [
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "A_Attack",
          "duration_seconds": 2.27,
          "approx_frames_30fps": 68,
          "events": [
            {
              "time": 1.13,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "A_Default",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "A_Idle",
          "duration_seconds": 1.47,
          "approx_frames_30fps": 44,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "A_Move",
          "duration_seconds": 1.67,
          "approx_frames_30fps": 50,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "A_Revive_1",
          "duration_seconds": 0.76667,
          "approx_frames_30fps": 23,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "A_Revive_2",
          "duration_seconds": 1.33,
          "approx_frames_30fps": 40,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "A_Revive_3",
          "duration_seconds": 3,
          "approx_frames_30fps": 90,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Attack_Begin_1",
          "duration_seconds": 0.66667,
          "approx_frames_30fps": 20,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Attack_Begin_2",
          "duration_seconds": 0.66667,
          "approx_frames_30fps": 20,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Attack_End_1",
          "duration_seconds": 0.23333,
          "approx_frames_30fps": 7,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Attack_End_2",
          "duration_seconds": 0.23333,
          "approx_frames_30fps": 7,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Attack_Loop_1",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [
            {
              "time": 0.1,
              "name": "OnAttack",
              "string": ""
            },
            {
              "time": 0.3,
              "name": "OnAttack",
              "string": ""
            },
            {
              "time": 0.5,
              "name": "OnAttack",
              "string": ""
            },
            {
              "time": 0.7,
              "name": "OnAttack",
              "string": ""
            },
            {
              "time": 0.9,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Attack_Loop_2",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [
            {
              "time": 0.1,
              "name": "OnAttack",
              "string": ""
            },
            {
              "time": 0.3,
              "name": "OnAttack",
              "string": ""
            },
            {
              "time": 0.5,
              "name": "OnAttack",
              "string": ""
            },
            {
              "time": 0.7,
              "name": "OnAttack",
              "string": ""
            },
            {
              "time": 0.9,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Default_1",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Default_2",
          "duration_seconds": 0,
          "approx_frames_30fps": 0,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Die_1",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Die_2",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Idle_1",
          "duration_seconds": 2.93,
          "approx_frames_30fps": 88,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Idle_2",
          "duration_seconds": 2.93,
          "approx_frames_30fps": 88,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Leave_1",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Leave_2",
          "duration_seconds": 1,
          "approx_frames_30fps": 30,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Move_1",
          "duration_seconds": 1.67,
          "approx_frames_30fps": 50,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Move_2",
          "duration_seconds": 1.47,
          "approx_frames_30fps": 44,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Skill_Begin_1",
          "duration_seconds": 0.6,
          "approx_frames_30fps": 18,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Skill_Begin_2",
          "duration_seconds": 0.6,
          "approx_frames_30fps": 18,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Skill_End_1",
          "duration_seconds": 1.93,
          "approx_frames_30fps": 58,
          "events": [
            {
              "time": 0.66667,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Skill_End_2",
          "duration_seconds": 1.93,
          "approx_frames_30fps": 58,
          "events": [
            {
              "time": 0.66667,
              "name": "OnAttack",
              "string": ""
            }
          ],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Skill_Loop_1",
          "duration_seconds": 4,
          "approx_frames_30fps": 120,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Skill_Loop_2",
          "duration_seconds": 4,
          "approx_frames_30fps": 120,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Start_1",
          "duration_seconds": 1.6,
          "approx_frames_30fps": 48,
          "events": [],
          "scope": "MT-1_to_MT-10"
        },
        {
          "enemy_id": "enemy_1567_pope",
          "enemy_name": "“圣徒”",
          "animation": "B_Start_2",
          "duration_seconds": 1.67,
          "approx_frames_30fps": 50,
          "events": [],
          "scope": "MT-1_to_MT-10"
        }
      ],
      "folder": "01_originals/enemies/enemy_1567_pope_“圣徒”",
      "source": "https://prts.wiki/w/“圣徒”"
    }
  ],
  "events": [
    {
      "label": "主选 · 圣堂相会",
      "path": "04_event_background/primary_approach_to_sanctuary.png"
    },
    {
      "label": "备选 · 圣徒现身",
      "path": "04_event_background/alternate_saint_encounter.png"
    },
    {
      "label": "备选 · 仪式深处",
      "path": "04_event_background/alternate_ritual_depth.png"
    }
  ]
};
if (typeof module !== 'undefined') module.exports = MT_ASSETS;
