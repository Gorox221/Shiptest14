ent-BaseNuclearReactor = ядерный реактор
    .desc = Корпус ядерного реактора со слотами для топливных и вспомогательных стержней. Эй, разве один из таких не взорвался когда-то?

ent-NuclearReactorCrew = { ent-BaseNuclearReactor }
    .desc = { ent-BaseNuclearReactor.desc }

ent-NuclearReactorNormal = { ent-NuclearReactorCrew }
    .desc = { ent-NuclearReactorCrew.desc }

ent-NuclearReactorEmpty = { ent-NuclearReactorCrew }
    .suffix = Пустой
    .desc = { ent-NuclearReactorCrew.desc }

ent-NuclearReactorRandom = { ent-NuclearReactorCrew }
    .suffix = Случайная загрузка
    .desc = { ent-NuclearReactorCrew.desc }

ent-NuclearReactorMeltdown = { ent-NuclearReactorCrew }
    .suffix = Расплав
    .desc = { ent-NuclearReactorCrew.desc }

ent-NuclearReactorMelted = { ent-NuclearReactorCrew }
    .suffix = Расплавлен
    .desc = Корпус ядерного реактора, давно расплавленный. Всё ещё светится от остаточного тепла и радиации.

ent-NuclearReactorSmall = малый ядерный реактор
    .desc = { ent-BaseNuclearReactor.desc }

ent-NuclearReactorSmallRandom = { ent-NuclearReactorSmall }
    .suffix = Случайная загрузка
    .desc = { ent-NuclearReactorSmall.desc }

ent-NuclearReactorSmallMelted = { ent-NuclearReactorSmall }
    .suffix = Расплавлен
    .desc = { ent-NuclearReactorMelted.desc }

ent-NuclearReactorSalvage = { ent-NuclearReactorCrew }
    .suffix = Утиль
    .desc = { ent-NuclearReactorCrew.desc }

ent-NuclearReactorNormalSalvage = { ent-NuclearReactorNormal }
    .suffix = Утиль
    .desc = { ent-NuclearReactorNormal.desc }

ent-NuclearReactorEmptySalvage = { ent-NuclearReactorEmpty }
    .suffix = Утиль
    .desc = { ent-NuclearReactorEmpty.desc }

ent-NuclearReactorRandomSalvage = { ent-NuclearReactorRandom }
    .suffix = Утиль
    .desc = { ent-NuclearReactorRandom.desc }

ent-NuclearReactorMeltedSalvage = { ent-NuclearReactorMelted }
    .suffix = Утиль
    .desc = { ent-NuclearReactorMelted.desc }

ent-NuclearReactorSmallSalvage = { ent-NuclearReactorSmall }
    .suffix = Утиль
    .desc = { ent-NuclearReactorSmall.desc }

ent-NuclearReactorSmallRandomSalvage = { ent-NuclearReactorSmallRandom }
    .suffix = Утиль
    .desc = { ent-NuclearReactorSmallRandom.desc }

ent-NuclearReactorSmallMeltedSalvage = { ent-NuclearReactorSmallMelted }
    .suffix = Утиль
    .desc = { ent-NuclearReactorSmallMelted.desc }

ent-NuclearDebrisChunk = ядерные обломки
    .desc = Вы не видите графит на полу. Вы в шоке. Обратитесь в медотсек.

ent-ReactorFlowArrow = { "" }
    .desc = { "" }

ent-ReactorSmallFlowArrow = { "" }
    .desc = { "" }

ent-ReactorGasPipe = { "" }
    .desc = { "" }
