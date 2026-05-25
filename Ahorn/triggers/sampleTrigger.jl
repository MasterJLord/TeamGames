module practiceModSampleTrigger

using ..Ahorn, Maple

@mapdef Trigger "practiceMod/SampleTrigger" SampleTrigger(
    x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight,
    sampleProperty::Integer=0
)

const placements = Ahorn.PlacementDict(
    "Sample Trigger (practiceMod)" => Ahorn.EntityPlacement(
        SampleTrigger,
        "rectangle",
    )
)

end