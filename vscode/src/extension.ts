import { promises as fs } from 'node:fs';
import * as vscode from 'vscode';
import { TRAIT_PRESETS, findPreset, presetWeights } from './core/traitPresets';
import { TraitIds } from './core/traitIds';
import { Decorator } from './decorator';
import { setBuildReaction } from './render/effects';
import { importVisualStudioSettings, visualStudioSettingsPath } from './settingsImport';
import { describe, registerHover } from './ui/hover';
import { Gallery } from './ui/gallery';

/** How long after a build the reactive braces keep reacting. */
const REACTION_WINDOW_MS = 25000;

export function activate(context: vscode.ExtensionContext): void {
    const decorator = new Decorator();
    context.subscriptions.push(decorator);
    context.subscriptions.push(registerHover(decorator));
    context.subscriptions.push(watchBuilds(decorator));

    context.subscriptions.push(
        vscode.workspace.onDidChangeConfiguration((e) => {
            if (e.affectsConfiguration('identityBraces.traits')) {
                Gallery.refresh();
            }
        }),
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('identityBraces.toggle', () => toggle('enabled')),
        vscode.commands.registerCommand('identityBraces.toggleMotion', () => toggle('enableMotion')),
        vscode.commands.registerCommand('identityBraces.openGallery', () => Gallery.show(context.extensionUri)),
        vscode.commands.registerCommand('identityBraces.applyPreset', () => applyPreset()),
        vscode.commands.registerCommand('identityBraces.whoIsThis', () => whoIsThis(decorator)),
        vscode.commands.registerCommand('identityBraces.importFromVisualStudio', () => importFromVisualStudio()),
    );
}

/**
 * Copies the Visual Studio extension's settings over.
 *
 * Both extensions describe the same braces, so anyone running both should only have to
 * decide once which of the eighty-six traits they want. Everything mapped is written,
 * including the values that match the default — see {@link importVisualStudioSettings}.
 */
async function importFromVisualStudio(): Promise<void> {
    const source = visualStudioSettingsPath();

    let ini: string;
    try {
        ini = await fs.readFile(source, 'utf8');
    } catch {
        vscode.window.showWarningMessage(
            `No Visual Studio settings found at ${source}. Open Tools → Options → Identity Braces once to create it.`,
        );
        return;
    }

    const { settings, notes } = importVisualStudioSettings(ini);
    const config = vscode.workspace.getConfiguration('identityBraces');

    for (const [key, value] of Object.entries(settings)) {
        await config.update(key, value, vscode.ConfigurationTarget.Global);
    }

    Gallery.refresh();

    const enabled = Object.values(settings.traits as Record<string, number>).filter((v) => v > 0).length;
    const summary = `Imported ${Object.keys(settings).length - 1} settings and ${enabled} traits from Visual Studio.`;

    if (notes.length === 0) {
        vscode.window.showInformationMessage(summary);
        return;
    }

    // The notes are the whole reason this is a command rather than a copy: what did not come
    // across is more useful to know than what did.
    const chosen = await vscode.window.showInformationMessage(summary, 'What did not come across?');
    if (chosen) {
        vscode.window.showWarningMessage(notes.join('\n\n'), { modal: true });
    }
}

export function deactivate(): void {
    // Everything is in context.subscriptions.
}

async function toggle(key: 'enabled' | 'enableMotion'): Promise<void> {
    const config = vscode.workspace.getConfiguration('identityBraces');
    const next = !config.get(key, true);
    await config.update(key, next, vscode.ConfigurationTarget.Global);

    vscode.window.setStatusBarMessage(
        key === 'enabled'
            ? next
                ? 'Identity Braces: on'
                : 'Identity Braces: off'
            : next
              ? 'Identity Braces: motion on'
              : 'Identity Braces: motion off',
        2000,
    );
}

async function applyPreset(): Promise<void> {
    const picked = await vscode.window.showQuickPick(
        TRAIT_PRESETS.map((p) => ({ label: p.name, detail: p.description, id: p.id })),
        { placeHolder: 'Trait preset' },
    );

    if (!picked) {
        return;
    }

    const preset = findPreset(picked.id);
    if (preset) {
        // Every trait is written, including the zeroes: a preset that only wrote its
        // non-zero entries would leave whatever the last one turned on still running.
        await vscode.workspace
            .getConfiguration('identityBraces')
            .update('traits', presetWeights(preset), vscode.ConfigurationTarget.Global);
    }
}

function whoIsThis(decorator: Decorator): void {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
        return;
    }

    const description = describe(decorator, editor.document, editor.selection.active);
    vscode.window.showInformationMessage(description ?? 'No brace at the cursor.');
}

/**
 * Green sparks for a build that passed, a grey droop for one that did not.
 *
 * Wired up unconditionally but costing nothing until the trait is switched on: the listener
 * is one event handler, and with the trait at zero no brace ever asks what the last build
 * did. The Visual Studio extension advises build events lazily for the same reason.
 *
 * A task is not quite a build, but it is the closest thing VS Code has that every project
 * type agrees on, and a failing test run is as good an excuse to sulk as a failing compile.
 */
function watchBuilds(decorator: Decorator): vscode.Disposable {
    let timer: NodeJS.Timeout | undefined;

    const listener = vscode.tasks.onDidEndTaskProcess((e) => {
        const wanted = decorator.currentSettings.scan.traitWeights.some(
            (w) => w.id === TraitIds.BuildReactive && w.percent > 0,
        );

        if (!wanted) {
            return;
        }

        setBuildReaction(e.exitCode === 0 ? 'succeeded' : 'failed');
        decorator.refreshAll();

        if (timer) {
            clearTimeout(timer);
        }

        timer = setTimeout(() => {
            setBuildReaction(null);
            decorator.refreshAll();
        }, REACTION_WINDOW_MS);
    });

    return new vscode.Disposable(() => {
        if (timer) {
            clearTimeout(timer);
        }

        listener.dispose();
    });
}
