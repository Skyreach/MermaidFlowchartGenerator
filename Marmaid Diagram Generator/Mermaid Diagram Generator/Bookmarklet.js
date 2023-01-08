javascript:(function() {
    let Nodes = {};
    class Route {
        constructor(source, destination, via, style) {
            this.Src = source;
            this.Dest = destination;
            this.Via = via;
            this.Style = style;
        }
    }
    async function Clipboard(text) {
        await navigator.clipboard.writeText(text);
    }

    function AlertCButton(message, copyText, buttonText) {
        let alert = document.createElement("div");
        alert.innerHTML = message;
        alert.style.display = "block";
        alert.style.position = "fixed";
        alert.style.zIndex = "1";
        alert.style.left = "50%";
        alert.style.top = "50%";
        alert.style.transform = "translate(-50%, -50%)";
        alert.style.backgroundColor = "#fff";
        alert.style.border = "1px solid #000";
        alert.style.padding = "20px";
        alert.style.boxShadow = "0 0 10px #000";

        let button = document.createElement("button");
        button.innerHTML = buttonText;
        button.style.marginTop = "10px";
        button.addEventListener("click", async function() {
            await Clipboard(copyText);
            alert.style.display = "none";
        });
        alert.appendChild(button);

        document.body.appendChild(alert);
    }

    AlertCButton("Prompt Made", Test1(), "Copy");
    function Test1() {
        let numNodes = Math.floor(Math.random() * (21 - 7)) + 7;
        let minEdges = numNodes - 1;
        let maxEdges = numNodes * 2 - 5;
        let numEdges = Math.floor(Math.random() * (maxEdges - minEdges + 1)) + minEdges;
        let nodeLabels = GetLocationNames(numNodes);
        let edgeLabels = GetLocationNames(numEdges);
        nodeLabels.forEach(nodeLabel => {
            Nodes[nodeLabel] = new Node(nodeLabel);
        });
        let routes = MakeRoutes(nodeLabels, edgeLabels);
        let chart = MakeChart(routes);
        console.log(chart);
        
        return chart;
    }

    function MakeChart(routes) {
        let chart = "flowchart TD;\n";
        for (const route of routes) {
            let srcHash = HashString(route.Src);
            let destHash = HashString(route.Dest);
            let sourceWeightStyled = (() => {
                switch (Nodes[route.Src].Weight) {
                    case 1:
                        return `(${route.Src})`;
                    case 2:
                        return `([${route.Src}])`;
                    case 3:
                        return `[[${route.Src}]]`;
                    case 4:
                        return `((${route.Src}))`;
                    default:
                        return `(((${route.Src})))`;
                }
            })();
            let destWeightStyled = (() => {
                switch (Nodes[route.Dest].Weight) {
                    case 1:
                        return `(${route.Dest})`;
                    case 2:
                        return `([${route.Dest}])`;
                    case 3:
                        return `[[${route.Dest}]]`;
                    case 4:
                        return `((${route.Dest}))`;
                    default:
                        return `(((${route.Dest})))`;
                }
            })();
            let viaStyled = route.Style === 'dotted' ? `-.${route.Via}.->` : `--${route.Via}---`;
            chart += `${srcHash}${sourceWeightStyled} ${viaStyled} ${destHash}${destWeightStyled}\n`;
        }
        return chart;
    }

    function MakeRoutes(nodeLabels, edgeLabels) {
        let usedNodes = nodeLabels.reduce((acc, nodeLabel) => {
            acc[nodeLabel] = false;
            return acc;
        }, {});
        let routes = [];
        for (const edgeLabel of edgeLabels) {
            if (Object.values(usedNodes).every(x => x)) {
                Merge(routes, edgeLabel);
            }
            else {
                let node1 = GetNode(nodeLabels, usedNodes);
                let node2 = GetNode(nodeLabels, usedNodes);
                let route = new Route(node1, node2, edgeLabel, GetStyle());
                routes.push(route);
            }
        }
        return routes;
    }

    function Merge(routes, edgeLabel) {
        let route1 = FindRoute(routes, 1) || FindRoute(routes, 2) || routes[Math.floor(Math.random() * routes.length)];
        let otherRoutes = routes.filter(x => x !== route1);
        let route2 = FindRoute(otherRoutes, 1) || FindRoute(otherRoutes, 2) || otherRoutes[Math.floor(Math.random() * otherRoutes.length)];
        let src = Math.floor(Math.random() * 2) === 0 ? route1.Src : route1.Dest;
        let dest = Math.floor(Math.random() * 2) === 0 ? route2.Src : route2.Dest;
        
        Nodes[src].Weight++;
        Nodes[dest].Weight++;
        routes.push(new Route(src, dest, edgeLabel, GetStyle()));
    }

    function FindRoute(routes, weight) {
        let filteredRoutes = routes.filter(r => Nodes[r.Src].Weight === weight || Nodes[r.Dest].Weight === weight);
        return filteredRoutes.shift();
    }

    function GetStyle() {
        return Math.random() < 0.2 ? 'dotted' : '';
    }

    function GetNode(nodeLabels, usedNodes) {
        let node = nodeLabels[Math.floor(Math.random() * nodeLabels.length)];
        if (Object.values(usedNodes).every(x => x)) {
            return node;
        }
        if (usedNodes[node]) {
            node = GetNode(nodeLabels, usedNodes);
        }
        usedNodes[node] = true;
        return node;
    }

    function HashString(str) {
        if (typeof str !== 'string' || str.length === 0) {
            return 0;
        }
        let hash = 0;
        for (let i = 0; i < str.length; i++) {
            hash = (hash << 5) - hash + str.charCodeAt(i);
            hash &= hash;
        }
        return Math.abs(hash);
    }

    function Node(label) {
        this.Label = label;
        this.Weight = 1;
    }

    function GetLocationNames(numNames) {
        let names = [];
        for (let i = 0; i < numNames; i++) {
            names.push(MakeName().replace('- ', '-'));
        }
        return names;
    }

    function MakeName() {
        let descriptions = ["Adamantine", "Aerial ", "Amphibious ", "Ancient ", "Arachnid ", "Astrological ", "Asymmetrical ",
            "Bizarre ", "Black ", "Bleak ", "Blue ", "Bronze ", "Buried ", "Celestial ", "Circuitous ", "Circular ",
            "Clay ", "Coiled ", "Collapsing ", "Concealed ", "Contaminated ", "Convoluted ", "Corroded ",
            "Criminal ", "Crimson ", "Crooked ", "Crude ", "Crumbling ", "Crystalline ", "Curious ", "Cursed ",
            "Cyclopean ", "Decaying ", "Deceptive ", "Decomposing ", "Defiled ", "Demolished ", "Demonic ",
            "Desolate ", "Destroyed ", "Devious ", "Diamond ", "Dilapidated ", "Disorienting ", "Divided ",
            "Dormant ", "Double ", "Dream-", "Earthen ", "Ebony ", "Eldritch ", "Elliptical ", "Enchanted ",
            "Enclosed ", "Entombed ", "Eroding ", "Ethereal ", "Fertile ", "Fortified ", "Fortress-", "Glittering ",
            "Grey ", "Hidden ", "High ", "Invulnerable ", "Isolated ", "Labyrinthine ", "Living ", "Moaning ",
            "Mud-", "Octagonal ", "Painted ", "Pearly ", "Pod-", "Poisoned ", "Quaking ", "Remade ", "Ruined ",
            "Rune-", "Sea-swept ", "Silent ", "Spiraling ", "Star-", "Storm-tossed ", "Sub-", "Sunken ", "Tall ",
            "Temporal ", "Three-Part ", "Titanic ", "Towering ", "Toxic ", "Treasure-", "Triangular ", "Unearthed ",
            "Unfinished ", "Unnatural ", "Urban ", "Watery ", "Wooden ", "Airborne ", "Aromatic ", "Azure ",
            "Belowground ", "Bone-", "Breathing ", "Brooding ", "Bubbling ", "Calcified ", "Cliff-", "Coastal ",
            "Conquered ", "Contemplation-", "Cruel ", "Cryptic ", "Cunning ", "Dank ", "Dark ", "Deadly ", "Death-",
            "Dimensional ", "Diseased ", "Drilling ", "Emerald ", "Erratic ", "Fabrication-", "Factory-", "Fear-",
            "Feeding ", "Flesh-", "Fossilized ", "Frightful ", "Gas-", "Granite ", "Green ", "Harvest-",
            "Heliotropic ", "Horned ", "Horrid ", "Hunting ", "Hydroponic ", "Industrial ", "Intermittent ",
            "Intriguing ", "Inverted ", "Lethargy-", "Levitating ", "Limestone ", "Midnight ", "Monastic ",
            "Mosaic ", "Mountain ", "Murder-", "Nest-", "Obsidian ", "Offshore ", "Orb-", "Perilous ",
            "Philosophical ", "Platform ", "Poorly-built ", "Pulsing ", "Putrid ", "Ramshackle ", "Red ",
            "Reversible ", "Sacrificial ", "Sapphire ", "Scarlet ", "Seaweed-", "Sentient ", "Sex-", "Shadow-",
            "Ship-", "Shunned ", "Singular ", "Sinister ", "Slaying-", "Temporary ", "Tumbled ", "Twilight ",
            "Unsealed ", "Unstable ", "Unthinkable ", "Vertical ", "Vile ", "Wailing ", "Walled ", "Waterborne ",
            "Weird ", "White "];
        let structures = ["Abbey o_t", "Aerie o_t", "Asylum o_t", "Aviary o_t", "Barracks o_t", "Bastion o_t",
            "Bazaar o_t", "Bluffs o_t", "Brewery o_t", "Bridge o_t", "Cairn o_t", "Canyon o_t",
            "Carnival o_t", "Castle o_t", "Cathedral o_t", "Cellars o_t", "Chapel o_t",
            "Chapterhouse o_t", "Church o_t", "City o_t", "Cliffs o_t", "Cloister o_t",
            "Cocoon o_t", "Coliseum o_t", "Contrivance o_t", "Cottage o_t", "Court o_t",
            "Crags o_t", "Craters o_t", "Crypt o_t", "Demi-plane o_t", "Dens o_t",
            "Dimension o_t", "Domain o_t", "Dome o_t", "Dungeons o_t", "Dwelling o_t",
            "Edifice o_t", "Fane o_t", "Farm o_t", "Forest o_t", "Forge o_t", "Fortress o_t",
            "Foundry o_t", "Galleon o_t", "Galleries o_t", "Garden o_t", "Garrison o_t",
            "Generator o_t", "Glade o_t", "Globe o_t", "Grotto o_t", "Hall o_t", "Halls o_t",
            "Harbor o_t", "Hatcheries o_t", "Haven o_t", "Hill o_t", "Hive o_t", "Holt o_t",
            "House o_t", "Hut o_t", "Island o_t", "Isles o_t", "Jungle o_t", "Keep o_t",
            "Kennels o_t", "Labyrinth o_t", "Lair o_t", "Lighthouse o_t", "Lodgings o_t",
            "Manse o_t", "Mansion o_t", "Marsh o_t", "Maze o_t", "Megalith o_t", "Mill o_t",
            "Mines o_t", "Monastery o_t", "Monolith o_t", "Mounds o_t", "Necropolis o_t",
            "Nest o_t", "Obelisk o_t", "Outpost o_t", "Pagoda o_t", "Palace o_t", "Pavilion o_t",
            "Pits o_t", "Prison o_t", "Pyramid o_t", "Rift o_t", "Sanctuary o_t", "Sanctum o_t",
            "Shrine o_t", "Spire o_t", "Stockades o_t", "Stronghold o_t", "Tower o_t",
            "Zeppelin o_t", "Cradle o_t", "Domains o_t", "Plane o_t", "Webs o_t"];
        let featurePrefix = ["Ant-", "Ape-", "Baboon-", "Bat-", "Beetle-", "Bitter", "Blood", "Bone-", "Brain", "Broken", "Bronze",
            "Burned", "Cabalistic", "Carnal", "Caterpillar-", "Centipede-", "Changing", "Chaos-", "Cloud-",
            "Cockroach-", "Crimson", "Crippled", "Crocodile-", "Dark", "Death-", "Decayed", "Deceitful", "Deluded",
            "Dinosaur-", "Diseased", "Dragonfly-", "Dread", "Elemental", "Elephant-", "Feathered", "Fiery", "Flame",
            "Flying", "Ghostly", "Gluttonous", "Gnarled", "Half-breed", "Heart-", "Hive", "Hollow", "Horned",
            "Howling", "Hunchback", "Hyena-", "Ice", "Immoral", "Immortal", "Imprisoned", "Insane", "Insatiable",
            "Iron", "Jackal-", "Jade", "Jewel", "Leech-", "Legendary", "Leopard-", "Lesser", "Lion-", "Loathsome",
            "Lunar", "Mad", "Mammoth-", "Man-eating", "Mantis-", "Many-legged", "Mist-", "Monkey-", "Moth-",
            "Mutant", "Ooze", "Outlawed", "Polluted", "Rat-", "Reawakened", "Resurrected", "Sabertooth", "Scarlet",
            "Scorched", "Secret", "Shadow", "Shattered", "Skeletal", "Slave", "Slime- S", "Slug-", "Snail-",
            "Snake-", "Twisted", "Undead", "Unholy", "Unseen", "Wasp-", "Worm-", "Zombie", "Armored", "Army o_t",
            "Artificial", "Bandit", "Bear", "Brain-", "Breeding", "Clan o_t", "Cloned", "Conjoined", "Cursed",
            "Demonic", "Deranged", "Enchanted", "Enslaved", "Feral", "Flame-", "Forest", "Frost", "Genius", "Giant",
            "Grotesque", "Guardian", "Hallucinogenic", "Hellish", "Horde o_t", "Horrific", "Hybrid", "Insidious",
            "Lava", "Leeching", "Mammoth", "Massive", "Master", "Mastermind", "Mechanical", "Mental", "Mind",
            "Minions o_t", "Moon-", "Narcotic", "Poisonous", "Predatory", "Raider-", "Reaver", "Sabertoothed",
            "Sand-", "Scheming", "Sea-", "Slime-", "Smoke", "Spell-", "Summoned", "Tribe o_t", "Vampiric",
            "Villainous", "Water", "Winged", "Wounded", "Wraith-"];
        let featureSuffix = ["Abbot", "Actor", "Alchemist", "Altar", "Apparition", "Apprentice", "Assassin", "Beast", "Behemoth",
            "Binder", "Bishop", "Breeder", "Brood", "Brotherhood", "Burrower", "Caller", "Captive", "Ceremony",
            "Chalice", "Changeling", "Chanter", "Circlet", "Clan", "Collector", "Combiner", "Congregation",
            "Coronet", "Crafter", "Crawler", "Creator", "Creature", "Crown", "Cult", "Cultists", "Daughter",
            "Demon", "Device", "Dreamer", "Druid", "Egg", "Emissary", "Emperor", "Executioner", "Exile",
            "Experimenter", "Eye", "Father", "Gatherer", "God", "Goddess", "Golem", "Grail", "Guardian", "Head",
            "Horde", "Hunter", "Hunters", "Hybrid", "Idol", "Jailer", "Keeper", "Killer", "King", "Knight", "Lich",
            "Lord", "Mage", "Magician", "Maker", "Master", "Monks", "Mother", "People", "Priest", "Priesthood",
            "Prince", "Princess", "Puppet", "Reaver", "Resurrectionist", "Scholar", "Seed", "Shaper", "Sisterhood",
            "Slitherer", "Society", "Son", "Sorcerer", "Sorceress", "pawn", "Star", "Statue", "Surgeon", "Tree",
            "Tribe", "Walker", "Warlord", "Weaver", "Whisperer", "Wizard", "Artifact", "Automaton", "Basilisk",
            "Bats", "Berserkers", "Cannibal", "Centaur", "Chieftain of Goblins", "Chimera", "Cleric", "Cockatrice",
            "Colossus", "Cyclops", "Demigod", "Displacer", "Djinni", "Doppelganger", "Dragon", "Efreet", "Eyeball",
            "Frog", "Fungus", "Gargoyles", "Genie", "Ghosts", "Ghouls", "Giants", "Griffon", "Hag", "Harpies",
            "Hornets", "Horror", "Hounds", "Hydra", "Infiltrator", "Insect", "Larva", "Lycanthrope", "Manticore",
            "Medusa", "Minotaurs", "Monster", "Mummy", "Mushroom", "Naga", "Nomads", "Octopus", "Ogres", "Oozes",
            "Pirates", "Priests", "Puddings", "Rakshasa", "Rats", "Salamander", "Satyr", "Scorpion", "Serpent",
            "Shaman", "Shaman o_t Orcs", "Simulacrum", "Skeletons", "Slimes", "Spawn", "Sphinx", "Spiders",
            "Spirits", "Titan", "Toad", "Troglodytes", "Trolls", "Tyrant", "Warlord o_t Orcs", "Wasps", "Witch",
            "Wolves", "Worgs", "Worm", "Wyrm", "Wyvern", "Yeti", "Zombies"];
        let description = descriptions[Math.floor(Math.random() * descriptions.length)];
        let structure = structures[Math.floor(Math.random() * structures.length)];
        let feature1 = featurePrefix[Math.floor(Math.random() * featurePrefix.length)];
        let feature2 = featureSuffix[Math.floor(Math.random() * featureSuffix.length)];
        return `${description}${structure} ${feature1} ${feature2}`;
    }
})();