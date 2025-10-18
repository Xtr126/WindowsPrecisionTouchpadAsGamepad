package xtr.keymapper.server.windows

import android.view.Display
import android.view.MotionEvent
import xtr.keymapper.server.Input

class TouchpadInputInjector {
    var verbose: Boolean = false
    private val input = Input(Display.DEFAULT_DISPLAY)

    fun inject(
        prevContacts: List<TouchpadContact>,
        currContacts: List<TouchpadContact>
    ) {
        for (contact in currContacts) {
            with(contact) {


                val action = if (prevContacts.any { it.contactId == contactId }) {
                                val prevContact = prevContacts.first { it.contactId == contactId }

                                if (!prevContact.tip && tip) MotionEvent.ACTION_DOWN
                                else if (tip) MotionEvent.ACTION_MOVE
                                else MotionEvent.ACTION_UP
                            } else {
                                MotionEvent.ACTION_DOWN
                            }

                if (verbose) println("injectEvent x=$x, y=$y, action=${MotionEvent.actionToString(action)}, $contactId")

                injectEvent(x.toFloat(), y.toFloat(), action, contact.contactId)
            }
        }
    }



    fun injectEvent(x: Float, y: Float, action: Int, pointerId: Int) {
        val pressure = when (action) {
            MotionEvent.ACTION_DOWN -> 1.0f
            else -> 0.0f
        }

        input.injectTouch(action, pointerId, pressure, x, y);
    }
}