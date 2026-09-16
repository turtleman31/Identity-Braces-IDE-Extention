package identitybraces.editor

import com.intellij.execution.ExecutionListener
import com.intellij.execution.process.ProcessHandler
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.editor.EditorFactory
import com.intellij.openapi.editor.event.EditorFactoryEvent
import com.intellij.openapi.editor.event.EditorFactoryListener
import com.intellij.task.ProjectTaskListener
import com.intellij.task.ProjectTaskManager
import com.intellij.util.concurrency.AppExecutorUtil
import identitybraces.render.BuildReaction
import identitybraces.render.BuildReactions
import java.util.concurrent.TimeUnit

/** Attaches Identity Braces to every editor as it opens. */
class BraceEditorFactoryListener : EditorFactoryListener {
    override fun editorCreated(event: EditorFactoryEvent) {
        EditorBraces.attach(event.editor)
    }

    override fun editorReleased(event: EditorFactoryEvent) {
        EditorBraces.detach(event.editor)
    }
}

/**
 * Feeds `buildreactive` its news.
 *
 * Two sources, because the IDEs disagree about what a build is: IntelliJ, CLion and Rider
 * route Build through the project task system; a run configuration that compiles as it goes
 * only reports through the execution listener. Either one counts — a failing test run is as
 * good an excuse to sulk as a failing compile.
 */
class BuildTaskListener : ProjectTaskListener {
    override fun finished(result: ProjectTaskManager.Result) {
        if (result.isAborted) {
            return
        }

        react(if (result.hasErrors()) BuildReaction.Failed else BuildReaction.Succeeded)
    }
}

class RunConfigurationListener : ExecutionListener {
    override fun processTerminated(executorId: String, env: ExecutionEnvironment, handler: ProcessHandler, exitCode: Int) {
        react(if (exitCode == 0) BuildReaction.Succeeded else BuildReaction.Failed)
    }
}

private fun react(reaction: BuildReaction) {
    BuildReactions.set(reaction)
    repaintEverything()

    // The reaction ends on its own clock, and a sulking brace must not stay grey until
    // something else happens to repaint its line.
    val remaining = BuildReactions.endsAt - System.currentTimeMillis() + 100
    AppExecutorUtil.getAppScheduledExecutorService().schedule(::repaintEverything, remaining, TimeUnit.MILLISECONDS)
}

private fun repaintEverything() {
    ApplicationManager.getApplication().invokeLater({
        for (editor in EditorFactory.getInstance().allEditors) {
            EditorBraces.of(editor)?.repaintVisible()
        }
    }, ModalityState.any())
}
