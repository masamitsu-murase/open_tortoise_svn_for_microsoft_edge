
require "pathname"
require "fileutils"

def main
    base = Pathname("AppX")
    base.rmtree if base.exist?
    FileUtils.cp_r("../OpenTortoiseSVN/OpenTortoiseSVN/bin/x64/Release/AppX", base)
    FileUtils.cp("../OpenTortoiseSVN/TortoiseSvnOpener/bin/Release/TortoiseSvnOpener.exe", base)
    FileUtils.cp("../OpenTortoiseSVN/TortoiseSvnOpener/bin/Release/TortoiseSvnOpener.pdb", base)
    FileUtils.cp_r("../extension", base + "Extension")

    appx_file = Pathname("OpenTortoiseSVN.appx")
    Dir.chdir(base) do
        file = Pathname("..\\#{appx_file}")
        file.rmtree if file.exist?
        ret = system("makeappx pack /d . /p #{file} /l")
        raise "makeappx error" unless ret
    end

    ret = system("signtool sign /fd sha256 /a /f private.pfx #{appx_file}")
    raise "signtool error" unless ret

    puts "Run certutil -addstore TrustedPeople public.cer"
    puts "Add-AppxPackage #{appx_file}"
end

Dir.chdir(__dir__) do
    main
end
