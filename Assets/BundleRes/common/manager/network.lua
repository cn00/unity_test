﻿
local CS = CS
local UnityEngine = CS.UnityEngine
local GameObject = UnityEngine.GameObject
local util = require "lua.utility.xlua.util"
local util = require "lua.utility.sprotoparser"
local socket = require "lua.socket.socket"
for k,v in pairs(socket) do print (k,v) end

local http = require "socket.http"
local ltn12 = require "socket.ltn12"
print("require http", http)

local response_body = {}  
local post_data = "post_data"  
local res, code, headers, status = http.request "http://localhost:8008"
-- local res, code, headers, status = http.request
-- {  
-- 	-- url = "http://anbolihua.iteye.com/blog/2316423",
-- 	url = "http://localhost:8008/index.html",
-- 	method = "GET",
-- 	-- headers =
-- 	-- {
-- 	-- 	["Content-Type"] = "text/html; charset=utf-8",
-- 	-- 	-- ["Content-Length"] = #post_data,
-- 	-- },
-- 	source = ltn12.source.string(post_data),
-- 	sink = ltn12.sink.table(response_body)
-- }
print("http.request", res, code, headers, status)
for k,v in pairs(headers) do print(k, v) end



local lpeg = require "lpeg"
local ffi = require "ffi"
-- print("--------")
--     for k, v in pairs(lpeg) do
--         print("lpeg", k, v)
--     end
    --[[
        dns	table: 0x14d1024c0
        skip	function: 0x139fafc90
        _SETSIZE	1024.0
        protect	function: 0x139faead0
        select	function: 0x139fb2cf0
        udp6	function: 0x139fb4e00
        sleep	function: 0x139fb42f0
        _VERSION	LuaSocket 3.0-rc1
        gettime	function: 0x139fb42a0
        __unload	function: 0x139fafcd0
        tcp	function: 0x139fb3c30
        tcp6	function: 0x139fb3c40
        connect	function: 0x139fb3c50
        udp	function: 0x139fb4df0
        newtry	function: 0x139faea70
    ]]
print("--------")

local Tag = "[network]"
local network = {
	Ip = "127.0.0.1",
	Port = "8888"
}
local this = network

-- local yield_return = util.async_to_sync(function (to_yield, callback)
--     mono:YieldAndCallback(to_yield, callback)
-- end)
-- function this.coroutine_demo()
--     util.coroutine_call(function()
--         print('network coroutine start!')
--         yield_return(UnityEngine.WaitForSeconds(1))
--         local obj = nil
--         yield_return(CS.AssetSys.Instance:GetAsset("ui/login/login.prefab", function(asset)
--             obj = asset
--         end))
--         local gameObj = GameObject.Instantiate(obj)
--     end)
-- end

--AutoGenInit Begin
function this.AutoGenInit() end
--AutoGenInit End

function this.Awake()
	this.AutoGenInit()
end

-- function this.OnEnable() end

-- local conn = assert(socket.connect(this.Ip, this.Port))
function this.Start()
	--assert(coroutine.resume(this.coroutine_demo()))
	-- print("conn:", conn)
	-- for k, v in pairs(getmetatable(conn)["__index"])do
	-- 	print("conn_meta", k, v)
	-- end
	--[[
		accept function: 0x13da99530
		bind function: 0x13da99660
		class tcp{client} 
		close function: 0x13da994f0
		connect function: 0x13da99730
		dirty function: 0x13da99820
		getfamily function: 0x13da99860
		getfd function: 0x13da998b0
		getoption function: 0x13da998f0
		getpeername function: 0x13da99930
		getsockname function: 0x13da99970
		getstats function: 0x13da999b0
		listen function: 0x13da99a10
		receive function: 0x13da99ac0
		send function: 0x13da99af0
		setfd function: 0x13da99b20
		setoption function: 0x13da99b60
		setpeername function: 0x13da99730
		setsockname function: 0x13da99660
		setstats function: 0x13da999e0
		settimeout function: 0x13da99ba0
		shutdown function: 0x13da99bd0
	]]
end

-- function this.FixedUpdate() end

-- function this.Update() end

-- function this.LateUpdate() end

-- function this.OnDestroy() end

function this.Destroy()
	GameObject.DestroyImmediate(mono.gameObject)
end
local function send_package(conn, pack)
	local package = string.pack(">s2", pack)
	print("send_package", #package, package)
	conn:send(package)
end

local function unpack_package(text)
	local size = #text
	if size < 2 then
		return nil, text
	end
	local s = text:byte(1) * 256 + text:byte(2)
	if size < s+2 then
		return nil, text
	end

	return text:sub(3,2+s), text:sub(3+s)
end

local function recv_package(last)
	local result
	result, last = unpack_package(last)
	if result then
		return result, last
	end
	local r, receive_status = conn:receive()
	print("recv_package", r, receive_status)
	if not r then
		return nil, last
	end
	if r == "" then
		error "Server closed"
	end
	return unpack_package(last .. r)
end

local session = 0

local function request( name, args, session )
	return string.format("%s:%s:%s", name, args, session)
end
local function send_request(name, args)
	session = session + 1
	local str = request(name, args, session)
	send_package(conn, str)
	print("Request:", session, name, args)
end

local last = ""

local function print_request(name, args)
	print("REQUEST", name)
	if args then
		for k,v in pairs(args) do
			print(k,v)
		end
	end
end

local function print_response(session, args)
	print("RESPONSE", session)
	if args then
		for k,v in pairs(args) do
			print(k,v)
		end
	end
end

local function print_package(t, ...)
	if t == "REQUEST" then
		print_request(...)
	else
		assert(t == "RESPONSE")
		print_response(...)
	end
end
local function dispatch_package()
	while true do
		local v
		v, last = recv_package(last)
		if not v then
			break
		end

		print_package(host:dispatch(v))
	end
end

-- send_request("handshake")
-- send_request("set", { what = "hello", value = "world" })

-- while true do
-- 	dispatch_package()
-- 	local cmd = socket.readstdin()
-- 	if cmd then
-- 		if cmd == "quit" then
-- 			send_request("quit")
-- 		else
-- 			send_request("get", { what = cmd })
-- 		end
-- 	else
-- 		socket.usleep(100)
-- 	end
-- end

return network