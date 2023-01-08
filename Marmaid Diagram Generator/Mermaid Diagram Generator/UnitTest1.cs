using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Mermaid_Diagram_Generator
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public UnitTest1(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public Dictionary<string, Node> Nodes { get; set; } = new();

        [Theory]
        [InlineData("Test City")]
        [InlineData("Test Source City -- Test Path --> Test Destination City")]
        [InlineData("Test Source City -. Test Hidden Path .-> Test Destination City")]
        [InlineData("Test Source City -. Test Hidden Path .-> Test Destination City; Test City")]
        [InlineData("Test Source City -. Test Hidden Path .-> Test Destination City; Test Source City")]
        public void Init(string inbound)
        {
            var inputSplits = inbound.Split(";");
            var edgeSeparators = new List<string>() { "--", "-->", "-.", ".->" };
            var cityLabels = inputSplits.Where(input => !edgeSeparators.Any(input.Contains)).ToList();
            var knownRoutes = inputSplits.Where(input => edgeSeparators.Any(input.Contains)).ToList();

            // create a hashset of all the cities
            var cities = new HashSet<string>();
            foreach (var cityLabel in cityLabels)
            {
                cities.Add(cityLabel.Trim());
            }

            // City weight should be a minimum of 1, or the feature should be removed.
            var cityWeight = Math.Max(1u,
                3);
            foreach (var city in cities)
            {
                Nodes.Add(city,
                    new Node(city,
                        cityWeight));
            }

            // When we have specific cities, we need fewer nodes.
            var numNodes = new Random()
                .Next(7,
                    21 - cities.Count);

            var minEdges = numNodes - 1;
            /* My thought is to add more edges to compensate for the resistance. */
            var maxEdges = (int) (numNodes * 2 - 5 + cities.Count * (cityWeight - 1));

            var numEdges = new Random()
                .Next(minEdges,
                    maxEdges + 1);

            var nodeLabels = GetLocationNames(Math.Max(0,
                numNodes - cities.Count));

            var edgeLabels = GetLocationNames(numEdges);

            foreach (var nodeLabel in nodeLabels)
            {
                Nodes.Add(nodeLabel,
                    new Node(nodeLabel,
                        0));
            }

            nodeLabels.AddRange(cities);

            var routes = PopulateRoutes2(
                nodeLabels,
                edgeLabels);
            
            routes.AddRange(knownRoutes.Select(RouteFromString));

            var chart = GenerateChart(routes);

            _testOutputHelper.WriteLine(chart);
        }

        
        private Route RouteFromString(string route)
        {
            var separatorStartArray = new[] { "--", "-.", };
            var separatorEndArray = new[] { "-->", ".->" };
            var separatorPathArray = new[] { "--", "-.", "-->", ".->" };
            var sourceCity = route.Split(separatorStartArray, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            var path = route.Split(separatorPathArray, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
            var destinationCity = route.Split(separatorEndArray, StringSplitOptions.RemoveEmptyEntries)[1].Trim();
            
            // Ensure that the sourceCity and destinationCity exist inside Nodes, and add new nodes if they don't.
            if (!Nodes.ContainsKey(sourceCity))
            {
                Nodes.Add(sourceCity, new Node(sourceCity, 0));
            }
            if (!Nodes.ContainsKey(destinationCity))
            {
                Nodes.Add(destinationCity, new Node(destinationCity, 0));
            }

            return new Route
            {
                Source = sourceCity,
                Destination = destinationCity,
                Via = path,
                Style = route.Contains("-.")
                    ? "dotted"
                    : null,
            };
        }

        private string GenerateChart(List<Route> routes)
        {
            var chart = "flowchart TD;\n";
            foreach (var route in routes)
            {
                var srcHash = HashString(route.Source);
                var destHash = HashString(route.Destination);
                var sourceWeightStyled = Nodes[route.Source].Usage switch
                {
                    1 => $"({route.Source})",
                    2 => $"([{route.Source}])",
                    3 => $"[[{route.Source}]]",
                    4 => $"(({route.Source}))",
                    _ => $"((({route.Source})))",
                };
                var destWeightStyled = Nodes[route.Destination].Usage switch
                {
                    1 => $"({route.Destination})",
                    2 => $"([{route.Destination}])",
                    3 => $"[[{route.Destination}]]",
                    4 => $"(({route.Destination}))",
                    _ => $"((({route.Destination})))",
                };
                var viaStyled = route.Style == "dotted"
                    ? $"-.{route.Via}.->"
                    : $"--{route.Via}---";
                chart +=
                    $"{srcHash}{sourceWeightStyled} {viaStyled} {destHash}{destWeightStyled}\n";
            }

            return chart;
        }

        private List<Route> PopulateRoutes2(List<string> nodeLabels, List<string> edgeLabels)
        {
            var usedNodes = nodeLabels
                .ToDictionary(
                    nodeLabel => nodeLabel,
                    _ => false);

            var routes = new List<Route>();

            foreach (var edgeLabel in edgeLabels)
            {
                if (usedNodes.All(x => x.Value))
                {
                    HandleRouteMerge(
                        routes,
                        edgeLabel);
                }
                else
                {
                    var node1 = GetRandomNode(nodeLabels,
                        usedNodes);
                    var node2 = GetRandomNode(nodeLabels,
                        usedNodes);

                    var route = new Route
                    {
                        Source = node1,
                        Destination = node2,
                        Via = edgeLabel,
                        Style = GetStyle(),
                    };
                    routes.Add(route);
                }
            }

            return routes;
        }

        private void HandleRouteMerge(List<Route> routes, string edgeLabel)
        {
            var route1 = FindRoute(routes,
                             1) ??
                         (FindRoute(routes,
                              2) ??
                          routes[new Random().Next(0,
                              routes.Count)]);

            var otherRoutes = routes.Where(x => x != route1).ToList();
            var route2 = FindRoute(otherRoutes,
                             1) ??
                         (FindRoute(otherRoutes,
                              2) ??
                          otherRoutes[new Random().Next(0,
                              otherRoutes.Count)]);

            // if the source node or destination node have a weight of 1, use them.

            var src = PrioritizeWeightOne(route1);
            var dest = PrioritizeWeightOne(route2);

            ChangeNodeWeights(src);
            ChangeNodeWeights(dest);

            routes.Add(new Route
            {
                Source = src,
                Destination = dest,
                Via = edgeLabel,
                Style = GetStyle(),
            });
        }

        private string PrioritizeWeightOne(Route route)
        {
            string src;
            if (Nodes[route.Source].Weight == 1)
            {
                src = route.Source;
            }
            else if (Nodes[route.Destination].Weight == 1)
            {
                src = route.Destination;
            }
            else
            {
                src = new Random().Next(0,
                    2) == 0
                    ? route.Source
                    : route.Destination;
            }

            return src;
        }

        private void ChangeNodeWeights(string cityName)
        {
            if (Nodes[cityName].Resistance > 1)
            {
                Nodes[cityName].Resistance--;
            }
            else
            {
                Nodes[cityName].Weight++;
            }

            Nodes[cityName].Usage++;
        }

        private Route FindRoute(List<Route> routes, uint weight)
        {
            return routes.FirstOrDefault(r =>
                Nodes[r.Source].Weight == weight || Nodes[r.Destination].Weight == weight);
        }

        private static string GetStyle()
        {
            var style = new Random().NextDouble() < 0.2
                ? "dotted"
                : null;
            return style;
        }

        private static string GetRandomNode(List<string> nodeLabels, Dictionary<string, bool> usedNodes)
        {
            var node = nodeLabels[new Random()
                .Next(
                    0,
                    nodeLabels.Count)];

            // if all nodes are used, return the selected node
            if (usedNodes.All(x => x.Value))
            {
                return node;
            }

            if (usedNodes[node])
            {
                node = GetRandomNode(nodeLabels,
                    usedNodes);
            }

            usedNodes[node] = true;
            return node;
        }

        private static int HashString(string str)
        {
            if (str.GetType() != typeof(string) || str.Length == 0)
            {
                return 0;
            }

            var hash = 0;

            foreach (int charCode in str)
            {
                hash = (hash << 5) - hash + charCode;
                hash &= hash;
            }

            return Math.Abs(hash);
        }

        private class Route
        {
            public string Destination { get; set; }
            public string Source { get; set; }
            public string Style { get; set; }
            public string Via { get; set; }
        }

        public class Node
        {
            public Node(string label, uint resistance)
            {
                Label = label;
                Weight = 1;
                Usage = 1;
                Resistance = resistance;
            }

            public uint Usage { get; set; }

            public string Label { get; set; }
            public uint Weight { get; set; } = 1;

            public uint Resistance { get; set; }
        }

        private static List<string> GetLocationNames(int numNames)
        {
            var names = new List<string>();
            for (var i = 0; i < numNames; i++)
            {
                names.Add(GenerateLocationName().Replace("- ",
                    "-"));
            }

            return names;
        }

        private static string GenerateLocationName()
        {
            var descriptions = new[]
            {
                "Adamantine", "Aerial ", "Amphibious ", "Ancient ", "Arachnid ", "Astrological ", "Asymmetrical ",
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
                "Weird ", "White ",
            };
            var structures = new[]
            {
                "Abbey of the", "Aerie of the", "Asylum of the", "Aviary of the", "Barracks of the", "Bastion of the",
                "Bazaar of the", "Bluffs of the", "Brewery of the", "Bridge of the", "Cairn of the", "Canyon of the",
                "Carnival of the", "Castle of the", "Cathedral of the", "Cellars of the", "Chapel of the",
                "Chapterhouse of the", "Church of the", "City of the", "Cliffs of the", "Cloister of the",
                "Cocoon of the", "Coliseum of the", "Contrivance of the", "Cottage of the", "Court of the",
                "Crags of the", "Craters of the", "Crypt of the", "Demi-plane of the", "Dens of the",
                "Dimension of the", "Domain of the", "Dome of the", "Dungeons of the", "Dwelling of the",
                "Edifice of the", "Fane of the", "Farm of the", "Forest of the", "Forge of the", "Fortress of the",
                "Foundry of the", "Galleon of the", "Galleries of the", "Garden of the", "Garrison of the",
                "Generator of the", "Glade of the", "Globe of the", "Grotto of the", "Hall of the", "Halls of the",
                "Harbor of the", "Hatcheries of the", "Haven of the", "Hill of the", "Hive of the", "Holt of the",
                "House of the", "Hut of the", "Island of the", "Isles of the", "Jungle of the", "Keep of the",
                "Kennels of the", "Labyrinth of the", "Lair of the", "Lighthouse of the", "Lodgings of the",
                "Manse of the", "Mansion of the", "Marsh of the", "Maze of the", "Megalith of the", "Mill of the",
                "Mines of the", "Monastery of the", "Monolith of the", "Mounds of the", "Necropolis of the",
                "Nest of the", "Obelisk of the", "Outpost of the", "Pagoda of the", "Palace of the", "Pavilion of the",
                "Pits of the", "Prison of the", "Pyramid of the", "Rift of the", "Sanctuary of the", "Sanctum of the",
                "Shrine of the", "Spire of the", "Stockades of the", "Stronghold of the", "Tower of the",
                "Zeppelin of the", "Cradle of the", "Domains of the", "Plane of the", "Webs of the",
            };
            var featurePrefix = new[]
            {
                "Ant-", "Ape-", "Baboon-", "Bat-", "Beetle-", "Bitter", "Blood", "Bone-", "Brain", "Broken", "Bronze",
                "Burned", "Cabalistic", "Carnal", "Caterpillar-", "Centipede-", "Changing", "Chaos-", "Cloud-",
                "Cockroach-", "Crimson", "Crippled", "Crocodile-", "Dark", "Death-", "Decayed", "Deceitful", "Deluded",
                "Dinosaur-", "Diseased", "Dragonfly-", "Dread", "Elemental", "Elephant-", "Feathered", "Fiery", "Flame",
                "Flying", "Ghostly", "Gluttonous", "Gnarled", "Half-breed", "Heart-", "Hive", "Hollow", "Horned",
                "Howling", "Hunchback", "Hyena-", "Ice", "Immoral", "Immortal", "Imprisoned", "Insane", "Insatiable",
                "Iron", "Jackal-", "Jade", "Jewel", "Leech-", "Legendary", "Leopard-", "Lesser", "Lion-", "Loathsome",
                "Lunar", "Mad", "Mammoth-", "Man-eating", "Mantis-", "Many-legged", "Mist-", "Monkey-", "Moth-",
                "Mutant", "Ooze", "Outlawed", "Polluted", "Rat-", "Reawakened", "Resurrected", "Sabertooth", "Scarlet",
                "Scorched", "Secret", "Shadow", "Shattered", "Skeletal", "Slave", "Slime- S", "Slug-", "Snail-",
                "Snake-", "Twisted", "Undead", "Unholy", "Unseen", "Wasp-", "Worm-", "Zombie", "Armored", "Army of the",
                "Artificial", "Bandit", "Bear", "Brain-", "Breeding", "Clan of the", "Cloned", "Conjoined", "Cursed",
                "Demonic", "Deranged", "Enchanted", "Enslaved", "Feral", "Flame-", "Forest", "Frost", "Genius", "Giant",
                "Grotesque", "Guardian", "Hallucinogenic", "Hellish", "Horde of the", "Horrific", "Hybrid", "Insidious",
                "Lava", "Leeching", "Mammoth", "Massive", "Master", "Mastermind", "Mechanical", "Mental", "Mind",
                "Minions of the", "Moon-", "Narcotic", "Poisonous", "Predatory", "Raider-", "Reaver", "Sabertoothed",
                "Sand-", "Scheming", "Sea-", "Slime-", "Smoke", "Spell-", "Summoned", "Tribe of the", "Vampiric",
                "Villainous", "Water", "Winged", "Wounded", "Wraith-",
            };

            var featureSuffix = new[]
            {
                "Abbot", "Actor", "Alchemist", "Altar", "Apparition", "Apprentice", "Assassin", "Beast", "Behemoth",
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
                "Shaman", "Shaman of the Orcs", "Simulacrum", "Skeletons", "Slimes", "Spawn", "Sphinx", "Spiders",
                "Spirits", "Titan", "Toad", "Troglodytes", "Trolls", "Tyrant", "Warlord of the Orcs", "Wasps", "Witch",
                "Wolves", "Worgs", "Worm", "Wyrm", "Wyvern", "Yeti", "Zombies",
            };

            var description = descriptions[new Random().Next(descriptions.Length)];
            var structure = structures[new Random().Next(structures.Length)];
            var feature1 = featurePrefix[new Random().Next(featurePrefix.Length)];
            var feature2 = featureSuffix[new Random().Next(featureSuffix.Length)];

            var outputMessage = $"{description}{structure} {feature1} {feature2}";
            return outputMessage;
        }
    }
}