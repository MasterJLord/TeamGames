rm TeamGames.zip
mv Source/obj/Debug temp
zip -r TeamGames.zip everest.yaml license.txt README.md Graphics/* bin/* Loenn/* Source/*
mv temp Source/obj/Debug
