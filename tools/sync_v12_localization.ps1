param([string]$Root = (Split-Path -Parent $PSScriptRoot))

$ErrorActionPreference = 'Stop'

function Update-LocFile([string]$Path, [hashtable]$Values, [string[]]$Remove = @()) {
    $json = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($key in $Remove) {
        $json.PSObject.Properties.Remove($key)
    }
    foreach ($entry in $Values.GetEnumerator()) {
        if ($json.PSObject.Properties.Name -contains $entry.Key) {
            $json.($entry.Key) = $entry.Value
        } else {
            $json | Add-Member -NotePropertyName $entry.Key -NotePropertyValue $entry.Value
        }
    }
    $json | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Path -Encoding utf8NoBOM
}

function Card-Loc([hashtable]$Target, [string]$Id, [string]$Title, [string]$Description, [string]$Prompt = '') {
    $Target["AK_EXUSIAI_CARD_$Id.title"] = $Title
    $Target["AK_EXUSIAI_CARD_$Id.description"] = $Description
    $Target["AK_EXUSIAI_CARD_$Id.smartDescription"] = $Description
    if ($Prompt) { $Target["AK_EXUSIAI_CARD_$Id.selectionScreenPrompt"] = $Prompt }
}

function Remove-BlueMarkupFromCardDescriptions([string]$Path) {
    $json = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($property in $json.PSObject.Properties) {
        if ($property.Name -match '\.(description|smartDescription)$') {
            $property.Value = [regex]::Replace(
                [string]$property.Value,
                '\[blue\](.*?)\[/blue\]',
                '$1')
        }
    }
    $json | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Path -Encoding utf8NoBOM
}

$zhs = @{}
Card-Loc $zhs 'STAR' '★' '造成{Damage:diff()}点伤害。\n若本次攻击消耗[gold]弹药[/gold]，升级你手牌中的{IfUpgraded:show:所有|[blue]1[/blue]张}牌。' '选择一张牌升级。'
Card-Loc $zhs 'ARMOR_PIERCING_ROUND' '穿甲弹' '使敌人失去所有[gold]格挡[/gold]。\n造成{Damage:diff()}点伤害。\n若本次攻击消耗[gold]弹药[/gold]，给予{VulnerablePower:diff()}层[gold]易伤[/gold]。'
Card-Loc $zhs 'ARMED_ESCORT' '武装护送' '造成{Damage:diff()}点伤害。\n获得[gold]中转[blue]1[/blue][/gold]开心小花。'
Card-Loc $zhs 'ROCK_N_ROLL' '摇滚！' '造成{Damage:diff()}点伤害。\n给予{Interference:diff()}层[gold]干扰[/gold]。'
Card-Loc $zhs 'MARKSMANSHIP' '射击技巧' '造成{Damage:diff()}点伤害。\n若本次攻击消耗[gold]弹药[/gold]，对随机敌人额外造成一次等量的伤害。'
Card-Loc $zhs 'CROSS_OF_DEVOTION' '虔心十字' '造成{Damage:diff()}点伤害[blue]2[/blue]次。\n为一张手牌添加[gold]天使[/gold]。' '选择一张手牌添加天使。'
Card-Loc $zhs 'VIOLENT_DELIVERY' '暴力运输' '使随机一个遗物获得[gold]快递{Delivery:diff()}[/gold]。\n造成{Damage:diff()}点伤害。'
Card-Loc $zhs 'FREE_DELIVERY' '免费派送' '抽[blue]1[/blue]张牌。\n获得[gold]中转{Transit:diff()}[/gold][gold]{MysteryRelic}[/gold]。'
Card-Loc $zhs 'QUICK_MAG' '快速弹匣' '获得{Ammo:diff()}发[gold]弹药[/gold]。\n抽[blue]2[/blue]张牌。'
Card-Loc $zhs 'HOLY_CITY_RADIANCE' '圣城威光' '除消耗堆，你每有一张[gold]天使[/gold]牌，此牌造成{Damage:diff()}点伤害1次。{InCombat:\n（命中{CalculatedHits:diff()}次）|}'
Card-Loc $zhs 'PARTY_TIME' '派对时间！' '造成{Damage:diff()}点伤害[blue]15[/blue]次。\n在你的手牌中加入一张[gold]睡眠不佳[/gold]。'
Card-Loc $zhs 'STEADFAST_HEART' '坚定之心' '获得{Block:diff()}点[gold]格挡[/gold]。\n在你的手牌中加入[blue]2[/blue]张[gold]受伤[/gold]。'
Card-Loc $zhs 'TARGET_THE_WEAK_SPOT' '瞄准弱点！' '如果一名敌人有负面状态，获得{FirepowerPower:diff()}点[gold]火力[/gold]。'
Card-Loc $zhs 'OUTSTANDING_GRADUATE' '优秀毕业生' '使所有敌人的[gold]干扰[/gold]层数{IfUpgraded:show:变为三倍|翻倍}。\n给予所有敌人1层[gold]干扰[/gold]。'
Card-Loc $zhs 'LOAN' '借贷' '获得{Gold:diff()}金币。\n将一张[gold]债务[/gold]加入你的抽牌堆。'
Card-Loc $zhs 'EMPATHY_FORM' '共感形态' '在回合开始时，为[blue]1[/blue]张手牌添加[gold]天使[/gold]。\n每打出[blue]1[/blue]张天使牌，抽[blue]1[/blue]张牌。'
Card-Loc $zhs 'HOLY_CITY_GUIDANCE' '圣城指引' '抽{Cards:diff()}张牌。'
Card-Loc $zhs 'MAY_THE_LORD_GUIDE_US' '愿主指引' '造成{Damage:diff()}点伤害。\n在你的手牌中加入一张[gold]{IfUpgraded:show:圣城指引+|圣城指引}[/gold]。'
Card-Loc $zhs 'BOTCHED_MODIFICATION' '改装不当' '对所有敌人造成{Damage:diff()}点伤害[blue]2[/blue]次。\n在你的手牌中加入一张[gold]悔恨[/gold]。'
Card-Loc $zhs 'WHAT_LIES_AHEAD' '前路何在？' '造成{Damage:diff()}点伤害[blue]3[/blue]次。\n在你的手牌中加入一张[gold]茫然[/gold]。'
Card-Loc $zhs 'SWEAR_ON_THIS_GUN' '以铳起誓' '造成{Damage:diff()}点伤害。\n在你的手牌中加入一张[gold]{IfUpgraded:show:圣城净化+|圣城净化}[/gold]。'
Card-Loc $zhs 'SHOOTING_MODE' '扫射模式' '造成{Damage:diff()}点伤害[blue]4[/blue]次。'
Card-Loc $zhs 'CHAOTIC_RAMPAGE' '混乱狂飙' '抽{Cards:diff()}张牌。\n在弃牌堆中加入一张[gold]歧途[/gold]。'
Card-Loc $zhs 'PENGUIN_STANDARD' '企鹅标快' '选择一个遗物获得[gold]快递{Delivery:diff()}[/gold]。\n获得[blue]2[/blue]个[gold]中转{Transit:diff()}[/gold]随机遗物。'
Card-Loc $zhs 'PENGUIN_INTERNATIONAL' '企鹅跨境' '选择一个遗物获得[gold]快递{Delivery:diff()}[/gold]。\n使{TransitCount:diff()}个中转遗物获得[gold]中转{Transit:diff()}[/gold]。'
Card-Loc $zhs 'ROCK_N_GOSPEL' '福音摇滚！' '给予所有敌人{WeakPower:diff()}层[gold]虚弱[/gold]和{Interference:diff()}层[gold]干扰[/gold]。'
Card-Loc $zhs 'GROUP_PHOTO' '大合照！' '将一张随机{IfUpgraded:show:升级的|}[gold]物流卡[/gold]加入你的手牌。'
Card-Loc $zhs 'COVENANT_OF_BULLETS' '铳弹协约' '使弃牌堆中1张牌获得[gold]天使[/gold]并加入手牌。\n结束你的回合。\n下回合开始时，获得等于你手牌数{IfUpgraded:show:+2|}的[gold]弹药[/gold]。' '选择一张牌加入手牌并添加天使。'
Card-Loc $zhs 'OVERLOADING_MODE' '过载模式' '进入[gold]过载[/gold]。\n本回合结束时，保留一半的[gold]弹药[/gold]。'
Card-Loc $zhs 'PIETY' '虔诚' '使抽牌堆中的一张牌获得[gold]天使[/gold]。\n抽{Cards:diff()}张牌。' '选择一张牌添加天使。'
Card-Loc $zhs 'PENGUIN_FREIGHT' '企鹅大件' '选择一个遗物获得[gold]快递{Delivery:diff()}[/gold]。\n获得{Block:diff()}点[gold]格挡[/gold]。\n你的下一回合开始时格挡不会消失。'
Card-Loc $zhs 'HOLY_CITY_EMBRACE' '圣城之拥' '每拥有[blue]1[/blue]张[gold]天使[/gold]牌，此牌使你获得{Block:diff()}点[gold]格挡[/gold][blue]1[/blue]次。{InCombat:\n（获得{CalculatedBlocks:diff()}次）|}'
Card-Loc $zhs 'HOLY_CITY_MERCY' '圣城怜悯' '获得[blue]8[/blue]点[gold]格挡[/gold]。\n为手牌中所有非[gold]天使[/gold]牌添加天使。'
Card-Loc $zhs 'TALENT' '天赋' '{IfUpgraded:show:给予所有敌人1层[gold]干扰[/gold]。\n|}敌人每有一层[gold]干扰[/gold]，受到的伤害提升10%。'
Card-Loc $zhs 'HOLY_CITY_CALLING' '圣城感召' '你在一回合内打出不少于[blue]3[/blue]张[gold]天使[/gold]牌时，在本回合获得[gold]翱翔[/gold]。'
Card-Loc $zhs 'THE_SAINTS_TRAVELS' '圣徒行记' '获得[blue]1[/blue]点[gold]火力[/gold]。\n这张牌在本局游戏中的火力值永久性增加[blue]1[/blue]。'
Card-Loc $zhs 'BRAWL' '喧闹' '给予{Interference:diff()}层[gold]干扰[/gold]。\n敌人身上每有2层[gold]干扰[/gold]，额外给予1层[gold]干扰[/gold]。'
Card-Loc $zhs 'DISRUPTIVE_STRIKE' '干扰打击' '造成{Damage:diff()}点伤害。\n敌人每有[blue]1[/blue]层[gold]干扰[/gold]，这张牌就额外攻击一次。{InCombat:\n（命中{CalculatedHits:diff()}次）|}'
Card-Loc $zhs 'BEWILDERED' '茫然' '[gold]悔恨[/gold]。'
Card-Loc $zhs 'WAYWARD' '歧途' '被消耗或在战斗中变化时，使你减少1点[gold]力量[/gold]与[gold]敏捷[/gold]。'

$eng = @{}
Card-Loc $eng 'STAR' '★' 'Deal {Damage:diff()} damage.\nIf this Attack spends [gold]Ammo[/gold], Upgrade {IfUpgraded:show:ALL cards|[blue]1[/blue] card} in your hand.' 'Choose a card to Upgrade.'
Card-Loc $eng 'ARMOR_PIERCING_ROUND' 'Armor-Piercing Round' 'Remove all [gold]Block[/gold] from the enemy.\nDeal {Damage:diff()} damage.\nIf this Attack spends [gold]Ammo[/gold], apply {VulnerablePower:diff()} [gold]Vulnerable[/gold].'
Card-Loc $eng 'ARMED_ESCORT' 'Armed Escort' 'Deal {Damage:diff()} damage.\nObtain a Happy Flower with [gold]Transit [blue]1[/blue][/gold].'
Card-Loc $eng 'ROCK_N_ROLL' "Rock n' Roll!" 'Deal {Damage:diff()} damage.\nApply {Interference:diff()} [gold]Interference[/gold].'
Card-Loc $eng 'MARKSMANSHIP' 'Marksmanship' 'Deal {Damage:diff()} damage.\nIf this Attack spends [gold]Ammo[/gold], deal that much damage once more to a random enemy.'
Card-Loc $eng 'CROSS_OF_DEVOTION' 'Cross of Devotion' 'Deal {Damage:diff()} damage [blue]2[/blue] times.\nGive a card in your hand [gold]Angel[/gold].' 'Choose a card in your hand to gain Angel.'
Card-Loc $eng 'VIOLENT_DELIVERY' 'Violent Delivery' 'Give a random relic [gold]Delivery {Delivery:diff()}[/gold].\nDeal {Damage:diff()} damage.'
Card-Loc $eng 'FREE_DELIVERY' 'Free Delivery' 'Draw [blue]1[/blue] card.\nGain a [gold]Transit {Transit:diff()}[/gold] [gold]{MysteryRelic}[/gold].'
Card-Loc $eng 'QUICK_MAG' 'Quick Mag' 'Gain {Ammo:diff()} [gold]Ammo[/gold].\nDraw [blue]2[/blue] cards.'
Card-Loc $eng 'HOLY_CITY_RADIANCE' 'Holy City Radiance' 'For each [gold]Angel[/gold] card you have outside your Exhaust Pile, deal {Damage:diff()} damage 1 time. {InCombat:\n(Hits {CalculatedHits:diff()} times)|}'
Card-Loc $eng 'PARTY_TIME' 'Party Time!' 'Deal {Damage:diff()} damage [blue]15[/blue] times.\nAdd a [gold]Poor Sleep[/gold] to your hand.'
Card-Loc $eng 'STEADFAST_HEART' 'Steadfast Heart' 'Gain {Block:diff()} [gold]Block[/gold].\nAdd [blue]2[/blue] [gold]Injuries[/gold] to your hand.'
Card-Loc $eng 'TARGET_THE_WEAK_SPOT' 'Target the Weak Spot!' 'If an enemy has a debuff, gain {FirepowerPower:diff()} [gold]Firepower[/gold].'
Card-Loc $eng 'OUTSTANDING_GRADUATE' 'Outstanding Graduate' '{IfUpgraded:show:Triple|Double} the [gold]Interference[/gold] on ALL enemies.\nApply 1 [gold]Interference[/gold] to ALL enemies.'
Card-Loc $eng 'LOAN' 'Loan' 'Gain {Gold:diff()} Gold.\nAdd a [gold]Debt[/gold] to your draw pile.'
Card-Loc $eng 'EMPATHY_FORM' 'Empathy Form' 'At the start of your turn, give [blue]1[/blue] card in your hand [gold]Angel[/gold].\nWhenever you play an Angel card, draw [blue]1[/blue] card.'
Card-Loc $eng 'HOLY_CITY_GUIDANCE' 'Holy City Guidance' 'Draw {Cards:diff()} cards.'
Card-Loc $eng 'MAY_THE_LORD_GUIDE_US' 'May the Lord Guide Us' 'Deal {Damage:diff()} damage.\nAdd a [gold]{IfUpgraded:show:Holy City Guidance+|Holy City Guidance}[/gold] to your hand.'
Card-Loc $eng 'BOTCHED_MODIFICATION' 'Botched Modification' 'Deal {Damage:diff()} damage to ALL enemies [blue]2[/blue] times.\nAdd a [gold]Regret[/gold] to your hand.'
Card-Loc $eng 'WHAT_LIES_AHEAD' 'What Lies Ahead?' 'Deal {Damage:diff()} damage [blue]3[/blue] times.\nAdd a [gold]Bewildered[/gold] to your hand.'
Card-Loc $eng 'SWEAR_ON_THIS_GUN' 'Swear on This Gun' 'Deal {Damage:diff()} damage.\nAdd a [gold]{IfUpgraded:show:Holy City Purge+|Holy City Purge}[/gold] to your hand.'
Card-Loc $eng 'SHOOTING_MODE' 'Shooting Mode' 'Deal {Damage:diff()} damage [blue]4[/blue] times.'
Card-Loc $eng 'CHAOTIC_RAMPAGE' 'Chaotic Rampage' 'Draw {Cards:diff()} cards.\nAdd a [gold]Wayward[/gold] to your Discard Pile.'
Card-Loc $eng 'PENGUIN_STANDARD' 'Penguin Standard' 'Choose a relic to gain [gold]Delivery {Delivery:diff()}[/gold].\nObtain [blue]2[/blue] random relics with [gold]Transit {Transit:diff()}[/gold].'
Card-Loc $eng 'PENGUIN_INTERNATIONAL' 'Penguin International' 'Choose a relic to gain [gold]Delivery {Delivery:diff()}[/gold].\nGive {TransitCount:diff()} Transit relics [gold]Transit {Transit:diff()}[/gold].'
Card-Loc $eng 'ROCK_N_GOSPEL' "Rock n' Gospel!" 'Apply {WeakPower:diff()} [gold]Weak[/gold] and {Interference:diff()} [gold]Interference[/gold] to ALL enemies.'
Card-Loc $eng 'GROUP_PHOTO' 'Group Photo!' 'Add a random {IfUpgraded:show:Upgraded |}[gold]Logistics Card[/gold] to your hand.'
Card-Loc $eng 'COVENANT_OF_BULLETS' 'Covenant of Bullets' 'Give 1 card in your discard pile [gold]Angel[/gold] and return it to your hand.\nEnd your turn.\nAt the start of your next turn, gain [gold]Ammo[/gold] equal to your hand size{IfUpgraded:show: plus 2|}.' 'Choose a card to return and give Angel.'
Card-Loc $eng 'OVERLOADING_MODE' 'Overloading Mode' 'Enter [gold]Overload[/gold].\nAt the end of this turn, retain half your [gold]Ammo[/gold].'
Card-Loc $eng 'PIETY' 'Piety' 'Give a card in your draw pile [gold]Angel[/gold].\nDraw {Cards:diff()} cards.' 'Choose a card to gain Angel.'
Card-Loc $eng 'PENGUIN_FREIGHT' 'Penguin Freight' 'Choose a relic to gain [gold]Delivery {Delivery:diff()}[/gold].\nGain {Block:diff()} [gold]Block[/gold].\nAt the start of your next turn, Block is not removed.'
Card-Loc $eng 'HOLY_CITY_EMBRACE' 'Holy City Embrace' 'For each [gold]Angel[/gold] card you have, gain {Block:diff()} [gold]Block[/gold] [blue]1[/blue] time. {InCombat:\n(Gain Block {CalculatedBlocks:diff()} times)|}'
Card-Loc $eng 'HOLY_CITY_MERCY' 'Holy City Mercy' 'Gain [blue]8[/blue] [gold]Block[/gold].\nGive every non-[gold]Angel[/gold] card in your hand Angel.'
Card-Loc $eng 'TALENT' 'Talent' '{IfUpgraded:show:Apply 1 [gold]Interference[/gold] to ALL enemies.\n|}Enemies take 10% more damage for each [gold]Interference[/gold] they have.'
Card-Loc $eng 'HOLY_CITY_CALLING' 'Holy City Calling' 'After you play at least [blue]3[/blue] [gold]Angel[/gold] cards in one turn, gain [gold]Soar[/gold] this turn.'
Card-Loc $eng 'THE_SAINTS_TRAVELS' "The Saints' Travels" 'Gain [blue]1[/blue] [gold]Firepower[/gold].\nPermanently increase the Firepower of this card by [blue]1[/blue] this run.'
Card-Loc $eng 'BRAWL' 'Brawl' 'Apply {Interference:diff()} [gold]Interference[/gold].\nApply 1 additional [gold]Interference[/gold] for every 2 [gold]Interference[/gold] the enemy already has.'
Card-Loc $eng 'DISRUPTIVE_STRIKE' 'Disruptive Strike' 'Deal {Damage:diff()} damage.\nAttack once more for each [blue]1[/blue] [gold]Interference[/gold] the enemy has. {InCombat:\n(Hits {CalculatedHits:diff()} times)|}'
Card-Loc $eng 'BEWILDERED' 'Bewildered' '[gold]Regret[/gold].'
Card-Loc $eng 'WAYWARD' 'Wayward' 'When Exhausted or transformed during combat, lose 1 [gold]Strength[/gold] and [gold]Dexterity[/gold].'

$zhsCards = Join-Path $Root 'AK_Exusiai/localization/zhs/cards.json'
$engCards = Join-Path $Root 'AK_Exusiai/localization/eng/cards.json'
$flash = @('AK_EXUSIAI_CARD_FLASHBANG.title', 'AK_EXUSIAI_CARD_FLASHBANG.description', 'AK_EXUSIAI_CARD_FLASHBANG.smartDescription')
Update-LocFile $zhsCards $zhs $flash
Update-LocFile $engCards $eng $flash
Remove-BlueMarkupFromCardDescriptions $zhsCards
Remove-BlueMarkupFromCardDescriptions $engCards

$zhsPowers = @{
    'AK_EXUSIAI_POWER_COVENANT_OF_BULLETS_POWER.title' = '铳弹协约'
    'AK_EXUSIAI_POWER_COVENANT_OF_BULLETS_POWER.description' = '下回合开始时，获得等于手牌数的弹药。剩余[blue]{Amount}[/blue]次。'
    'AK_EXUSIAI_POWER_COVENANT_OF_BULLETS_POWER.smartDescription' = '下回合开始时，获得等于手牌数的弹药。剩余[blue]{Amount}[/blue]次。'
    'AK_EXUSIAI_POWER_COVENANT_OF_BULLETS_BONUS_POWER.title' = '铳弹协约升级加成'
    'AK_EXUSIAI_POWER_COVENANT_OF_BULLETS_BONUS_POWER.description' = '铳弹协约的升级加成剩余[blue]{Amount}[/blue]次。'
    'AK_EXUSIAI_POWER_COVENANT_OF_BULLETS_BONUS_POWER.smartDescription' = '铳弹协约的升级加成剩余[blue]{Amount}[/blue]次。'
    'AK_EXUSIAI_POWER_OVERLOAD_AMMO_RETENTION_POWER.description' = '本回合结束时，保留一半的[gold]弹药[/gold]。'
    'AK_EXUSIAI_POWER_OVERLOAD_AMMO_RETENTION_POWER.smartDescription' = '本回合结束时，保留一半的[gold]弹药[/gold]。'
    'AK_EXUSIAI_POWER_INTERFERENCE_POWER.description' = '每层使攻击造成的伤害降低[blue]5%[/blue]，最多降低[blue]50%[/blue]。获得干扰时及每额外获得[blue]5[/blue]层干扰后，获得[gold]沉默[/gold]。'
    'AK_EXUSIAI_POWER_INTERFERENCE_POWER.smartDescription' = '每层使攻击造成的伤害降低[blue]5%[/blue]，最多降低[blue]50%[/blue]。获得干扰时及每额外获得[blue]5[/blue]层干扰后，获得[gold]沉默[/gold]。'
    'AK_EXUSIAI_POWER_EMPATHY_FORM_POWER.description' = '在回合开始时，选择[blue]{Amount}[/blue]张手牌获得天使。每打出一张天使牌，抽[blue]{Amount}[/blue]张牌。'
    'AK_EXUSIAI_POWER_EMPATHY_FORM_POWER.smartDescription' = '在回合开始时，选择[blue]{Amount}[/blue]张手牌获得天使。每打出一张天使牌，抽[blue]{Amount}[/blue]张牌。'
    'AK_EXUSIAI_POWER_EMPATHY_FORM_POWER.selectionScreenPrompt' = '选择一张手牌添加天使。'
}
$engPowers = @{
    'AK_EXUSIAI_POWER_COVENANT_OF_BULLETS_POWER.title' = 'Covenant of Bullets'
    'AK_EXUSIAI_POWER_COVENANT_OF_BULLETS_POWER.description' = 'At the start of your next turn, gain Ammo equal to your hand size. [blue]{Amount}[/blue] trigger(s) remaining.'
    'AK_EXUSIAI_POWER_COVENANT_OF_BULLETS_POWER.smartDescription' = 'At the start of your next turn, gain Ammo equal to your hand size. [blue]{Amount}[/blue] trigger(s) remaining.'
    'AK_EXUSIAI_POWER_COVENANT_OF_BULLETS_BONUS_POWER.title' = 'Covenant of Bullets Upgrade Bonus'
    'AK_EXUSIAI_POWER_COVENANT_OF_BULLETS_BONUS_POWER.description' = '[blue]{Amount}[/blue] upgraded Covenant of Bullets trigger(s) remaining.'
    'AK_EXUSIAI_POWER_COVENANT_OF_BULLETS_BONUS_POWER.smartDescription' = '[blue]{Amount}[/blue] upgraded Covenant of Bullets trigger(s) remaining.'
    'AK_EXUSIAI_POWER_OVERLOAD_AMMO_RETENTION_POWER.description' = 'At the end of this turn, retain half your [gold]Ammo[/gold].'
    'AK_EXUSIAI_POWER_OVERLOAD_AMMO_RETENTION_POWER.smartDescription' = 'At the end of this turn, retain half your [gold]Ammo[/gold].'
    'AK_EXUSIAI_POWER_INTERFERENCE_POWER.description' = 'Each stack reduces Attack damage dealt by [blue]5%[/blue], up to [blue]50%[/blue]. Gain [gold]Silence[/gold] when Interference is first gained and for every [blue]5[/blue] additional stacks.'
    'AK_EXUSIAI_POWER_INTERFERENCE_POWER.smartDescription' = 'Each stack reduces Attack damage dealt by [blue]5%[/blue], up to [blue]50%[/blue]. Gain [gold]Silence[/gold] when Interference is first gained and for every [blue]5[/blue] additional stacks.'
    'AK_EXUSIAI_POWER_EMPATHY_FORM_POWER.description' = 'At the start of your turn, choose [blue]{Amount}[/blue] cards in your hand to gain Angel. Whenever you play an Angel card, draw [blue]{Amount}[/blue] cards.'
    'AK_EXUSIAI_POWER_EMPATHY_FORM_POWER.smartDescription' = 'At the start of your turn, choose [blue]{Amount}[/blue] cards in your hand to gain Angel. Whenever you play an Angel card, draw [blue]{Amount}[/blue] cards.'
    'AK_EXUSIAI_POWER_EMPATHY_FORM_POWER.selectionScreenPrompt' = 'Choose a card in your hand to gain Angel.'
}
$oldPowerKeys = @(
    'AK_EXUSIAI_POWER_ROCK_N_GOSPEL_POWER.title','AK_EXUSIAI_POWER_ROCK_N_GOSPEL_POWER.description','AK_EXUSIAI_POWER_ROCK_N_GOSPEL_POWER.smartDescription','AK_EXUSIAI_POWER_ROCK_N_GOSPEL_POWER.selectionScreenPrompt'
)
Update-LocFile (Join-Path $Root 'AK_Exusiai/localization/zhs/powers.json') $zhsPowers $oldPowerKeys
Update-LocFile (Join-Path $Root 'AK_Exusiai/localization/eng/powers.json') $engPowers $oldPowerKeys

$zhsRelics = @{
    'AK_EXUSIAI_RELIC_FIREPOWER_FM.description' = '同一回合内每消耗[blue]4[/blue]发[gold]弹药[/gold]，获得[blue]1[/blue]点[gold]火力[/gold]。'
    'AK_EXUSIAI_RELIC_LORD_SERVER.description' = '你在战斗中每次获得诅咒牌时，获得{Energy:energyIcons()}。'
}
$engRelics = @{
    'AK_EXUSIAI_RELIC_FIREPOWER_FM.description' = 'Whenever you spend [blue]4[/blue] [gold]Ammo[/gold] in one turn, gain [blue]1[/blue] [gold]Firepower[/gold].'
}
Update-LocFile (Join-Path $Root 'AK_Exusiai/localization/zhs/relics.json') $zhsRelics
Update-LocFile (Join-Path $Root 'AK_Exusiai/localization/eng/relics.json') $engRelics

# Chinese card text follows the base game's convention: no spaces around
# numeric values or dynamic variables. Keep this mechanical pass scoped to
# localized JSON rather than altering English typography.
Get-ChildItem (Join-Path $Root 'AK_Exusiai/localization/zhs') -Filter '*.json' | ForEach-Object {
    $text = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8
    $text = [regex]::Replace($text, '([\p{IsCJKUnifiedIdeographs}，。；：、（）]) (?=(?:\[(?:blue|gold)\])?(?:\d|X|\{))', '$1')
    $text = [regex]::Replace($text, '((?:\d|X|\}|\[/blue\]|\[/gold\])) (?=[\p{IsCJKUnifiedIdeographs}，。；：、（）])', '$1')
    Set-Content -LiteralPath $_.FullName -Value $text -Encoding utf8NoBOM -NoNewline
}

function Add-BlueNumericMarkup([string]$Value) {
    $protected = [ordered]@{}
    $valueWithoutTokens = [regex]::Replace(
        $Value,
        '(\[blue\].*?\[/blue\]|\{[^{}]*\})',
        {
            param($match)
            $token = "@@AKTOKEN$($protected.Count)@@"
            $protected[$token] = $match.Value
            return $token
        })
    $valueWithoutTokens = [regex]::Replace(
        $valueWithoutTokens,
        '(?<![A-Za-z@])([+-]?\d+(?:\.\d+)?%?|X(?:\+\d+)?)',
        '[blue]$1[/blue]')
    foreach ($entry in $protected.GetEnumerator()) {
        $valueWithoutTokens = $valueWithoutTokens.Replace($entry.Key, $entry.Value)
    }
    return $valueWithoutTokens
}

Get-ChildItem (Join-Path $Root 'AK_Exusiai/localization/zhs') -Filter '*.json' |
    Where-Object { $_.Name -ne 'cards.json' } | ForEach-Object {
    $json = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($property in $json.PSObject.Properties) {
        if ($property.Name -match '\.(description|smartDescription)$') {
            $property.Value = Add-BlueNumericMarkup ([string]$property.Value)
        }
    }
    $json | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $_.FullName -Encoding utf8NoBOM
}
