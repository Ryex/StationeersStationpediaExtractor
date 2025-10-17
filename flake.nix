{
  description = "A Nix-flake-based C# .NET development environment";

  # GitHub URLs for the Nix inputs we're using
  inputs = {
    # Simply the greatest package repository on the planet
    nixpkgs.url = "github:NixOS/nixpkgs";
    # A set of helper functions for using flakes
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let 
        pkgs = import nixpkgs { inherit system; };
      
        deps = with pkgs; [
            zlib
            zlib.dev
            openssl
            dotnetCorePackages.dotnet_8.sdk
        ];
      in {
        devShells = {
          default = pkgs.mkShell {

            # Packages included in the environment
            buildInputs = with pkgs; [

              # Nuget
              dotnetPackages.Nuget

              #.Net
            ] ++ deps;
            
            NIX_LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath ([
                pkgs.stdenv.cc.cc
            ] ++ deps);

            NIX_LD = "${pkgs.stdenv.cc.libc_bin}/bin/ld.so";

            # Run when the shell is started up
            shellHook = ''
                DOTNET_ROOT="${pkgs.dotnetCorePackages.dotnet_8.sdk}";
                ${pkgs.dotnetCorePackages.dotnet_8.sdk}/bin/dotnet --version
            '';
          };
        };
      });
}
