
local Sizes = {
	"large",
	"medium",
	"small"
}
local BackgroundGear = {
	name = "TeamGames/BackgroundGear",
	depth = 9949,
	texture = "objects/TeamGames/WhiteGear/medium0",
	fieldInformation =
	{
		size =
		{
			options = Sizes,
			editable = false
		},
		color = 
		{
			fieldType = "color"
		}
	},
	placements = 
	{
		name = "medium gear",
		data = 
		{
			size = "medium",
			scrollspeed = 0.5,
			color = "000000"
		}
	}

}
-- function BackgroundGear.sprite(room, entity)

return BackgroundGear
