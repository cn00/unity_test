
--- Author: cn
--- Email: cool_navy@qq.com
--- Date: 2021/01/12 12:07:03
--- Description: 五子棋
--[[
-[ ] 人机对战
-[ ] 自动匹配
-[ ] 创建对局
-[ ] 选择对局:对战/旁观
]]

local G = _G
local CS = CS
local UnityEngine = CS.UnityEngine
local GameObject = UnityEngine.GameObject
local util = require "util"
local xutil = require "xlua.util"
local sqlite3 = require("lsqlite3")
local manager = AppGlobal.manager
local config = require("common.config.config")
local socket = require("socket.core")
local AppGlobal = AppGlobal

local dbpath = config.dbCachePath;
local userDbpath = config.userDbPath;

local yield_return = xutil.async_to_sync(function (to_yield, callback)
    mono:YieldAndCallback(to_yield, callback)
end)

-- index

local print = function ( ... )
    _G.print("gobang/index", ...)
end

local index = {
}
local this = index

function index.OnDestroy()
    if AppGlobal.Client then AppGlobal.Client.RemoveListeners(this.OnServerMsgType) end
end

--AutoGenInit Begin
--[[
请勿手动编辑此函数
手動でこの関数を編集しないでください。
DO NOT EDIT THIS FUNCTION MANUALLY.
لا يدويا تحرير هذه الوظيفة
]]
function this.AutoGenInit()
    this.bg_Image = bg:GetComponent(typeof(CS.UnityEngine.UI.Image))
    this.btnRoot_RectTransform = btnRoot:GetComponent(typeof(CS.UnityEngine.RectTransform))
    this.gameGround_Button = gameGround:GetComponent(typeof(CS.UnityEngine.UI.Button))
    this.gameGround_Button.onClick:AddListener(this.gameGround_OnClick)
    this.history_Button = history:GetComponent(typeof(CS.UnityEngine.UI.Button))
    this.history_Button.onClick:AddListener(this.history_OnClick)
    this.nameInput_Text = nameInput:GetComponent(typeof(CS.UnityEngine.UI.Text))
    this.p2cPlay_Button = p2cPlay:GetComponent(typeof(CS.UnityEngine.UI.Button))
    this.p2cPlay_Button.onClick:AddListener(this.p2cPlay_OnClick)
end
--AutoGenInit End

---对局大厅
function this.gameGround_OnClick()
    print('gameGround_OnClick')
    AppGlobal.SceneManager.push("gobang/hostList/hostList.prefab", {
        --parent = nil,
        autoMatch = true,
        matchType = 1, -- 0:主场， 1:客场, 2:观众
    }, true)
end -- gameGround_OnClick

---人机对战
function this.p2cPlay_OnClick()
    print('p2cPlay_OnClick')
    -- start a local server
    AppGlobal.Client.ConnectToServer("localhost", 9990, function(ok)
        if ok then
            print("ConnectToServer ok")
            AppGlobal.Client.AddListeners(this.OnServerMsgType)
            AppGlobal.Client.SendMsgt({
                type = "autoMatch"
            })
        end
    end)
end -- p2cPlay_OnClick

---历史战绩
function this.history_OnClick()
    print('history_OnClick')
    AppGlobal.SceneManager.push("gobang/history/history.prefab", nil, true)
end -- history_OnClick

function index.Awake()
    this.AutoGenInit()
end

function index.Start()
    btnRoot:SetActive(false)
    xutil.coroutine_call(function()
        yield_return(CS.AssetSys.GetAsset("font/fzkt/STKaiti.ttf"))

        -- chat emoji
        yield_return(CS.AssetSys.GetAsset(string.format("common/emoji/%d.png", 1)))

        local obj
        if(AppGlobal.Server == nil) then
            yield_return(CS.AssetSys.GetAsset("gobang/net/server.prefab", function(asset) obj = asset  end))
            GameObject.Instantiate(obj, AppGlobal.SceneManager.layer.back)
        end

        if(AppGlobal.Client == nil)then
            yield_return(CS.AssetSys.GetAsset("gobang/net/client.prefab", function(asset) obj = asset  end))
            GameObject.Instantiate(obj, AppGlobal.SceneManager.layer.back)
        end

        btnRoot:SetActive(true)

        -- init userdata db
        local sql
        yield_return(CS.AssetSys.GetAsset("gobang/sql/userdata.sql", function(asset)
            sql = asset.text
            print("userdata.sql", sql)
        end))
        local db = sqlite3.open(userDbpath);
        assert(sqlite3.OK == db:exec(sql), db:errmsg())
        db:close()
    end)
end

local function OnAutoMatch(msgt)
    print("OnAutoMatch", msgt)
    AppGlobal.SceneManager.push("gobang/match/match.prefab", {
        --matchType = "p2c",
        roomId = msgt.roomId,
        autoMatch = true,
        matchType = 1, -- 0:主场， 1:客场, 2:观众
    }, true)
end

index.OnServerMsgType = {
    ["autoMatch"]   = OnAutoMatch,
}

return index