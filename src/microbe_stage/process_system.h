#pragma once

#include "engine/component.h"
#include "engine/system.h"
#include "engine/touchable.h"
#include "engine/typedefs.h"

#include <boost/range/adaptor/map.hpp>
#include <vector>
#include <unordered_map>

namespace sol {
class state;
}

namespace thrive {

class ProcessorComponent : public Component {
    COMPONENT(Processor)

public:
    static void luaBindings(sol::state &lua);

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

    std::unordered_map<BioProcessId, float> process_capacities;
    std::unordered_map<CompoundId, std::tuple<float, float, float>> thresholds; // low, high, excess

    void
    setThreshold(CompoundId, float, float, float);
    void
    setLowThreshold(CompoundId, float);
    void
    setHighThreshold(CompoundId, float);
    void
    setVentThreshold(CompoundId, float);

    void
    setCapacity(BioProcessId, float);
};

class CompoundBagComponent : public Component {
    COMPONENT(CompoundBag)

public:
    static void luaBindings(sol::state &lua);

    CompoundBagComponent();

    void
    load(
        const StorageContainer& storage
    ) override;

    StorageContainer
    storage() const override;

    ProcessorComponent* processor = nullptr;
    std::string speciesName;
    std::unordered_map<CompoundId, float> compounds;

    void
    setProcessor(ProcessorComponent& processor, const std::string& speciesName);

    float
    getCompoundAmount(CompoundId);

    float
    takeCompound(CompoundId, float); // remove up to a certain amount of compound, returning how much was removed

    void
    giveCompound(CompoundId, float);

    float
    excessAmount(CompoundId); // return the amount of compound in excess

    float
    aboveLowThreshold(CompoundId id); // the amount of compound above low threshold
};

class ProcessSystem : public System {

public:
    static void luaBindings(sol::state &lua);

    /**
    * @brief Constructor
    */
    ProcessSystem();

    /**
    * @brief Destructor
    */
    ~ProcessSystem();

    /**
    * @brief Initializes the system
    *
    */
    void init(GameStateData* gameState) override;

    /**
    * @brief Shuts the system down
    */
    void shutdown() override;

    /**
    * @brief Updates the system
    */
    void update(int renderTime, int logicTime) override;
private:

    struct Implementation;
    std::unique_ptr<Implementation> m_impl;
};

}
