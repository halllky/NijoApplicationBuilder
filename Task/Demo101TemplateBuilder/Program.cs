if (args.Length != 1) {
    Console.Error.WriteLine("使い方: demo101-template-builder <作業フォルダのパス>");
    return 1;
}

try {
    Demo101TemplateBuilder.Demo101TemplatePruner.Prune(args[0]);
    return 0;
} catch (Exception ex) {
    Console.Error.WriteLine(ex.ToString());
    return 1;
}
