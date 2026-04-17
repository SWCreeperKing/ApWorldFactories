namespace ApWorldFactories.Games.Vampire_Survivors;

public static class CodeBank
{
    //todo: make an arcana needed setting for enemy sanity
    public const string GenEarly = """
                                   stages = [stage for stage in self.final_included_stages_list if stage != EUDAI]
                                   characters = self.final_included_characters_list

                                   if len(stages) > 1:
                                   	stages_to_choose_from = [stage for stage in stages if stage in normal_stages]

                                   	if len(stages_to_choose_from) == 0:
                                   		stages_to_choose_from = [stage for stage in stages if stage in bonus_stages]

                                   	if len(stages_to_choose_from) == 0:
                                   		stages_to_choose_from = [stage for stage in stages if stage in challenge_stages]

                                   	if len(stages_to_choose_from) == 0:
                                   		self.starting_stage = self.random.choice(stages)
                                   	else:
                                   		self.starting_stage = self.random.choice(stages_to_choose_from)
                                   else:
                                   	self.starting_stage = stages[0]

                                   if len(characters) > 1:  # choose lesser powerful characters to start with
                                   	characters_to_choose_from = [character for character in characters if character in non_special_characters]

                                   	if self.options.allow_secret_characters and len(characters_to_choose_from) == 0:
                                   		characters_to_choose_from = [character for character in characters if character in secret_characters]

                                   	if self.options.allow_megalo_characters and len(characters_to_choose_from) == 0:
                                   		characters_to_choose_from = [character for character in characters if character in megalo_characters]

                                   	if self.options.allow_unfair_characters and self.settings.allow_unfair_characters and len(
                                   			characters_to_choose_from) == 0:
                                   		characters_to_choose_from = [character for character in characters if character in unfair_characters]

                                   	self.starting_character = self.random.choice(characters_to_choose_from)
                                   else:
                                   	self.starting_character = characters[0]
                                   self.stage_goal_amount = len(stages)

                                   self.multiworld.push_precollected(self.create_item(f"Stage Unlock: {self.starting_stage}"))
                                   self.multiworld.push_precollected(self.create_item(f"Character Unlock: {self.starting_character}"))
                                   
                                   if not self.options.lock_arcanas_behind_item.value:
                                       self.multiworld.push_precollected(self.create_item(f"Gamemode Unlock: Arcanas"))
                                   if not self.options.lock_hurry_behind_item.value:
                                   	self.multiworld.push_precollected(self.create_item("Gamemode Unlock: Hurry"))
                                   """;

    public const string CheckOptions = """
                                       characters = options.get_included_characters(world)
                                       stages = options.get_included_stages(world)

                                       if not options.allow_secret_characters:
                                           characters = [character for character in characters if character not in secret_characters]

                                       if not options.allow_megalo_characters:
                                           characters = [character for character in characters if character not in megalo_characters]

                                       if not settings.allow_unfair_characters and options.allow_unfair_characters:
                                           options.allow_unfair_characters = AllowUnfairCharacters(False)
                                           raise_yaml_error(world.player_name,
                                                            "`allow_unfair_characters` can not be enabled unless the host,yaml setting 'allow_unfair_characters' is also enabled")

                                       if not options.allow_unfair_characters:
                                           characters = [character for character in characters if character not in unfair_characters]

                                       if len(stages) == 0:
                                           raise_yaml_error(world.player_name, "You must have more than 0 stages included")

                                       if len(characters) == 0:
                                           raise_yaml_error(world.player_name, "You must have more than 0 eligible characters included")

                                       if len(stages) > options.stage_pool_size > 0:
                                           stages = random.sample(stages, options.stage_pool_size)
                                           
                                       if len(characters) > options.character_pool_size > 0:
                                           characters = random.sample(characters, options.character_pool_size)
                                       
                                       if EUDAI not in stages and options.goal_requirement == 1:
                                           stages.append(EUDAI)

                                       if len(stages) == 1 and stages[0] == EUDAI:
                                           raise_yaml_error(world.player_name, f"{EUDAI} CAN NOT BE YOUR ONLY STAGE")

                                       random.shuffle(characters)
                                       random.shuffle(stages)
                                       world.final_included_characters_list = characters
                                       world.final_included_stages_list = stages
                                       
                                       if world.options.goal_requirement == 1:
                                         if world.ending_stage_count == 0:
                                       	   world.ending_stage_count = int(len(world.final_included_stages_list) * .75)
                                       """;
}