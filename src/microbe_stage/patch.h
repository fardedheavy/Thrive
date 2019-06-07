#pragma once

#include <Entities/Component.h>
#include <Entities/Components.h>
#include <Entities/System.h>
#include <microbe_stage/biomes.h>
#include <unordered_map>

namespace thrive {

class CellStageWorld;

//! An object that represents a patch
class Patch {
public:
    std::string name;
    size_t patchId;

    Patch(std::string name);
    virtual ~Patch();

    std::string
        getName();
    void
        setName(std::string name);

    Biome*
        getBiome();
    void
        setBiome(Biome* biome);

    size_t
        getId();

private:
    Biome* patchBiome = nullptr;
    std::vector<std::weak_ptr<Patch>> adjacentPatches;
};


class PatchManager {
public:
    PatchManager();

    Patch*
        getCurrentPatch();

protected:

private:
	std::unordered_map<size_t, std::shared_ptr<Patch>> patchMap;
    size_t currentPatchId = 0;

};

} // namespace thrive