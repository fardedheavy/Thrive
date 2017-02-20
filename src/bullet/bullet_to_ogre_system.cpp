#include "bullet/bullet_to_ogre_system.h"

#include "bullet/rigid_body_system.h"
#include "engine/game_state.h"
#include "engine/entity_filter.h"
#include "ogre/scene_node_system.h"
#include "scripting/luajit.h"

using namespace thrive;

void BulletToOgreSystem::luaBindings(
    sol::state &lua
){
    lua.new_usertype<BulletToOgreSystem>( "BulletToOgreSystem",
        
        sol::constructors<sol::types<>>(),

        sol::base_classes, sol::bases<System>()
    );
}

struct BulletToOgreSystem::Implementation {

    EntityFilter<
        RigidBodyComponent,
        OgreSceneNodeComponent
    > m_entities;
};


BulletToOgreSystem::BulletToOgreSystem()
  : m_impl(new Implementation())
{
}


BulletToOgreSystem::~BulletToOgreSystem() {}


void
BulletToOgreSystem::init(
    GameStateData* gameState
) {
    System::initNamed("BulletToOgreSystem", gameState);
    m_impl->m_entities.setEntityManager(gameState->entityManager());
}


void
BulletToOgreSystem::shutdown() {
    m_impl->m_entities.setEntityManager(nullptr);
    System::shutdown();
}


void
BulletToOgreSystem::update(int, int) {
    for (auto& value : m_impl->m_entities) {
        RigidBodyComponent* rigidBodyComponent = std::get<0>(value.second);
        OgreSceneNodeComponent* sceneNodeComponent = std::get<1>(value.second);
        auto& sceneNodeTransform = sceneNodeComponent->m_transform;
        auto& rigidBodyProperties = rigidBodyComponent->m_dynamicProperties;
        sceneNodeTransform.orientation = rigidBodyProperties.rotation;
        sceneNodeTransform.position = rigidBodyProperties.position;
        sceneNodeTransform.touch();
    }
}


