-- Run with Aseprite --batch --script-param project=... --script this-file.
-- Create a colour-only runtime variant; leave every original atlas and pose intact.
local project = assert(app.params.project, "project path required")
local out = project .. "/Assets/Resources/AnimationOverrides"
app.fs.makeDirectory(out)
local palette = {
  ["255,255,232"] = {235,255,255},
  ["255,225,120"] = {178,242,255},
  ["255,180,56"] = {111,224,248},
  ["244,119,34"] = {67,183,235},
  ["189,59,38"] = {43,119,201},
  ["118,43,48"] = {27,65,122}
}
local variants = {
  {"atlas_60823c2175d70fc39277", "Circe_Magic_Ball"},
  {"atlas_eb0b3579907153b6cf33", "Circe_Magic_Impact"}
}
for _, entry in ipairs(variants) do
  local src = app.open(project .. "/Assets/Resources/AnimationPack/Textures/" .. entry[1] .. ".png")
  assert(src, "missing atlas")
  app.transaction("Circe cyan palette", function()
    for _, cel in ipairs(src.cels) do
      local img = cel.image
      for px in img:pixels() do
        local old = px()
        local alpha = app.pixelColor.rgbaA(old)
        if alpha > 0 then
          local r, g, b = app.pixelColor.rgbaR(old), app.pixelColor.rgbaG(old), app.pixelColor.rgbaB(old)
          local replacement = assert(palette[r .. "," .. g .. "," .. b], "unexpected source colour")
          px(app.pixelColor.rgba(replacement[1], replacement[2], replacement[3], alpha))
        end
      end
    end
  end)
  src:saveCopyAs(out .. "/" .. entry[2] .. ".png")
  src:close()
end
print("CIRCE_MAGIC_PALETTE_EXPORTED")
