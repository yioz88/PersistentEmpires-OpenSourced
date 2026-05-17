# 持久帝国 开源项目

持久帝国（Persistent Empires）是一款《骑马与砍杀2：霸主》的多人游戏模组，它引入了一些新的游戏机制，让玩家可以进行角色扮演、团队战斗、部族战斗、农场经营、采矿等玩法。

## 系统要求

- Windows 服务器环境

- 数据库：MariaDB（推荐 10.4.27-MariaDB 版本）(https://mariadb.com/kb/en/installing-mariadb-msi-packages-on-windows/)
  或使用 MySQL8（8.0.36 版本应该可以正常工作）(https://dev.mysql.com/downloads/installer/)

- 骑马与砍杀2：霸主专用服务器（可通过 steamcmd 安装 https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip）

安装命令：
```
steamcmd.exe +force_install_dir "C:\Desktop\你的服务器文件夹路径" +login 你的steam用户名 "你的steam密码" +app_update 1863440 validate
```

## 安装步骤

1. 安装专用服务器后，你会在服务器目录中看到一个名为 `Modules` 的文件夹。

2. 将持久帝国的模块文件和文件夹解压到 `你的服务器路径/Modules/PersistentEmpires`

3. 将 `PersistentEmpires/bin/Win64_ShippingServer` 解压到 `你的服务器路径/bin/Win64_ShippingServer`

4. 安装一个 MySQL 数据库管理工具（Navicat、DBeaver、phpmyadmin 或 MySQL Workbench）

<p align="center">
  <img src="https://github.com/Heavybob/PersistentEmpires-OpenSourced/assets/4519067/e83817b5-a4e7-44a3-81c0-bb099206452a" alt="DBeaver 设置">
</p>
<p align="center"><em>DBeaver 设置界面</em></p>

5. 连接到你的 MariaDB 数据库并创建一个数据库，假设命名为 `pe_production`

<p align="center">
  <img src="https://github.com/Heavybob/PersistentEmpires-OpenSourced/assets/4519067/a7c801f7-92a7-430b-a77d-7ee90d3dcff5" alt="图片">
</p>
<p align="center"><em>已创建名为 pe_production 的数据库，其他数据库可以忽略</em></p>

6. 现在需要为你的 PE 服务器设置数据库连接。打开以下文件：
   `你的服务器文件夹/Modules/PersistentEmpires/ModuleData/Configs/SaveConfig.xml`

7. 编辑文件后，你会看到如下内容：

SaveConfig.xml
```xml
<DatabaseConfig>
	<ConnectionString>
		Server=localhost;User ID=root;Password=密码;Database=pe_production
	</ConnectionString>
</DatabaseConfig>
```

8. 设置数据库的 User ID、Password 和 Database 字段。

9. 需要在以下位置创建文件：`你的服务器路径/Modules/Native/persistent_empires.txt`

persistent_empires.txt
```txt
ServerName 持久帝国
GameType PersistentEmpires
Map pe_test3
CultureTeam1 khuzait
CultureTeam2 vlandia
AllowPollsToKickPlayers False
AllowPollsToBanPlayers False
AllowPollsToChangeMaps False
MapTimeLimit 60000
RespawnPeriodTeam1 5
RespawnPeriodTeam1 5
MinNumberOfPlayersForMatchStart 0
MaxNumberOfPlayers 500
DisableInactivityKick True
add_map_to_automated_battle_pool pe_test3
end_game_after_mission_is_over
start_game_and_mission
```

10. 创建你的启动批处理文件，准备开始运行。

```.bat
start DedicatedCustomServer.Starter.exe /dedicatedcustomserverconfigfile persistent_empires.txt /port 7211 /DisableErrorReporting /no_watchdog /tickrate 75 /multiplayer /dedicatedcustomserverauthtoken 在此处填入你的服务器验证令牌 _MODULES_*Native*Multiplayer*PersistentEmpires*_MODULES_
```

- 不要忘记设置 `/dedicatedcustomserverauthtoken`，你必须从 bannerlord 获取此令牌。
- 参考文档：https://moddocs.bannerlord.com/multiplayer/hosting_server/

- 确保在 persistent_empires.txt 中设置了你要使用的正确地图。

- 地图必须放置在 `Multiplayer` 模块中：`你的服务器路径/Modules/Multiplayer/SceneObj`，并且必须在 persistent_empires.txt 中使用 `add_map_to_automated_battle_pool` 命令才能在游戏内的下载面板中显示。

## 服务器配置

持久帝国允许用户在服务端配置一些选项。

配置文件位于 `你的服务器路径/Modules/PersistentEmpires/ModuleData/Configs/GeneralConfig.xml`

此文件可以为空，如果你需要可以使用以下配置示例，未定义的值将使用默认值。

GeneralConfig.xml
```xml
<GeneralConfig>
  <VoiceChatEnabled>true</VoiceChatEnabled>
  <StartingGold>1000</StartingGold>

  <!-- 自动重启设置 -->
  <AutorestartActive>true</AutorestartActive>
  <AutorestartIntervalHours>24</AutorestartIntervalHours>

  <!-- 银行设置 -->
  <BankAmountLimit>1000000</BankAmountLimit>

  <!-- 战斗日志系统 -->
  <CombatlogDuration>5</CombatlogDuration>

  <!-- 医生设置 -->
  <RequiredMedicineSkillForHealing>50</RequiredMedicineSkillForHealing>
  <MedicineHealingAmount>15</MedicineHealingAmount>
  <MedicineItemId>pe_doctorscalpel</MedicineItemId>

  <!-- 饥饿设置 -->
  <HungerInterval>72</HungerInterval>
  <HungerReduceAmount>1</HungerReduceAmount>
  <HungerRefillHealthLowerBoundary>25</HungerRefillHealthLowerBoundary>
  <HungerHealingAmount>10</HungerHealingAmount>
  <HungerHealingReduceAmount>5</HungerHealingReduceAmount>
  <HungerStartHealingUnderHealthPct>75</HungerStartHealingUnderHealthPct>

  <!-- 领主投票设置 -->
  <LordPollRequiredGold>1000</LordPollRequiredGold>
  <LordPollTimeOut>60</LordPollTimeOut>

  <!-- 政治系统 -->
  <WarDeclareTimeOut>30</WarDeclareTimeOut>
  <PeaceDeclareTimeOut>30</PeaceDeclareTimeOut>
  <MaxBannerLength>100</MaxBannerLength>

  <!-- 盗贼系统 -->
  <LockpickItem>pe_lockpick</LockpickItem>
  <PickpocketingItem>pe_stealing_dagger</PickpocketingItem>
  <RequiredPickpocketing>10</RequiredPickpocketing>
  <PoisonItemId>pe_poison_dagger</PoisonItemId>
  <AntidoteItemId>pe_antidote</AntidoteItemId>
  <PickpocketingPercentageThousands>10</PickpocketingPercentageThousands>
  <DeathMoneyDropPercentage>25</DeathMoneyDropPercentage>

  <!-- 其他设置 -->
  <AnimationsEnabled>true</AnimationsEnabled>
  <AgentLabelEnabled>true</AgentLabelEnabled> <!-- 玩家头顶显示旗帜 -->
  <DontOverrideMangonelHit>false</DontOverrideMangonelHit> <!-- 是否覆盖投石机对玩家的伤害 -->
  <NameChangeGold>5000</NameChangeGold> <!-- 改名所需金币 -->
  <NameChangeCooldownInSeconds>3600</NameChangeCooldownInSeconds> <!-- 改名冷却时间 -->
  <RepairTimeoutAfterHit>60</RepairTimeoutAfterHit> <!-- 受到伤害后的修复冷却时间 -->
  <DecapitationChance>25</DecapitationChance>
</GeneralConfig>
```

## API 接口

持久帝国提供了 API 功能。

通过它，你可以发送 GET/POST 请求来远程执行服务器上的功能。
如果你打算创建管理工具（如外部控制面板或 Discord 机器人），这将非常强大。

配置文件位于 `你的服务器路径/Modules/PersistentEmpires/ModuleData/Configs/ApiConfig.xml`

你需要生成一个密钥（secretkey）并使用该密钥生成 JWT 令牌。警告 - 不要使用示例密钥，请生成你自己的密钥。

ApiConfig.xml
```xml
<ApiConfig>
	<Port>3169</Port>
	<SecretKey>2b6f1a8c3e74d0f5e92a7bdcf5013c1e9f5aeb6a74066eb45d03f11a5b7486d13721b53223e9f4d87c66b32b6e3b5d4ff8c951b1b05619d1e2c2616c1f8d39ba</SecretKey>
</ApiConfig>
```

以下是使用 API 可以执行的操作：

```
POST /compensateplayer - 给玩家发放金币
{
    "PlayerId": "玩家ID",
    "Gold": 100
}

POST /kickplayer - 将玩家踢出服务器
{
    "PlayerId": "玩家ID"
}

POST /fadeplayer - 杀死玩家并删除其护甲
{
    "PlayerId": "玩家ID"
}

POST /unbanplayer - 解除对玩家的封禁
{
    "PlayerId": "玩家ID",
    "UnbanReason": "解除封禁的原因"
}

POST /banplayer - 封禁玩家
{
    "PlayerId": "玩家ID",
    "BanEndsAt": "2024-05-01T00:00:00",
    "BanReason": "封禁原因"
}

POST /announce - 向服务器内所有玩家发送公告
{
    "Message": "你的公告内容"
}

GET /servercap - 返回当前玩家数量

GET /restart - 发起服务器重启

GET /shutdown - 关闭服务器
```
