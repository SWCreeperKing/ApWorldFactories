namespace ApWorldFactories;

/*
###
# Non-world client methods
###

def launch_client(*args):
    from .ManualClient import launch as Main
    launch_subprocess(Main, name="Manual client")

class VersionedComponent(Component):
    def __init__(self, display_name: str, script_name: Optional[str] = None, func: Optional[Callable] = None, version: int = 0, file_identifier: Optional[Callable[[str], bool]] = None):
        super().__init__(display_name=display_name, script_name=script_name, func=func, component_type=Type.CLIENT, file_identifier=file_identifier)
        self.version = version

def add_client_to_launcher() -> None:
    version = 2024_07_09 # YYYYMMDD
    found = False
    for c in components:
        if c.display_name == "Manual Client":
            found = True
            if getattr(c, "version", 0) < version:  # We have a newer version of the Manual Client than the one the last apworld added
                c.version = version
                c.func = launch_client
                return
    if not found:
        components.append(VersionedComponent("Manual Client", "ManualClient", func=launch_client, version=version, file_identifier=SuffixIdentifier('.apmanual')))

add_client_to_launcher()

 */
public abstract class ManualBuildData(int directory, string gameName, string modFolder, string apWorld, string sheetId, string version, string apVersion = "0.6.5", string gameFolder = "") : BuildData(directory, gameName, modFolder, apWorld, sheetId, version, apVersion, gameFolder)
{
    
}