package xtr.keymapper.server;

import android.hardware.input.InputManager;
import android.os.Build;
import android.os.SystemClock;
import android.util.Log;
import android.view.InputDevice;
import android.view.InputEvent;
import android.view.MotionEvent;

import com.genymobile.scrcpy.Point;
import com.genymobile.scrcpy.Pointer;
import com.genymobile.scrcpy.PointersState;

import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;

public class Input {

    static Method injectInputEventMethod;
    static Object inputManager;
    static Method setDisplayIdMethod;

    private final PointersState pointersState = new PointersState();
    private final MotionEvent.PointerProperties[] pointerProperties = new MotionEvent.PointerProperties[PointersState.MAX_POINTERS];
    private final MotionEvent.PointerCoords[] pointerCoords = new MotionEvent.PointerCoords[PointersState.MAX_POINTERS];
    private final int displayId;
    private long lastTouchDown;


    private void initPointers() {
        for (int i = 0; i < PointersState.MAX_POINTERS; ++i) {
            MotionEvent.PointerProperties props = new MotionEvent.PointerProperties();
            props.toolType = MotionEvent.TOOL_TYPE_FINGER;

            MotionEvent.PointerCoords coords = new MotionEvent.PointerCoords();
            coords.orientation = 0;
            coords.size = 0;

            pointerProperties[i] = props;
            pointerCoords[i] = coords;
        }
    }

    public Input(int displayId) {
        this.displayId = displayId;
        initPointers();
    }

    public void injectTouch(int action, int pointerId, float pressure, float x, float y) {
        long now = SystemClock.uptimeMillis();
        Point point = new Point(x, y);

        int pointerIndex = pointersState.getPointerIndex(pointerId);
        if (pointerIndex == -1) {
            Log.e("InputService", "Too many pointers for touch event");
        }
        Pointer pointer = pointersState.get(pointerIndex);
        pointer.setPoint(point);
        pointer.setPressure(pressure);
        if (action == MotionEvent.ACTION_UP || action == MotionEvent.ACTION_HOVER_MOVE)
            pointer.setUp(true);
        else if (action == MotionEvent.ACTION_DOWN) pointer.setUp(false);

        int source = InputDevice.SOURCE_TOUCHSCREEN;

        int pointerCount = pointersState.update(pointerProperties, pointerCoords);

        if (pointerCount == 1) {
            if (action == MotionEvent.ACTION_DOWN) {
                lastTouchDown = now;
            }
        } else {
            // secondary pointers must use ACTION_POINTER_* ORed with the pointerIndex
            if (action == MotionEvent.ACTION_UP) {
                action = MotionEvent.ACTION_POINTER_UP |
                        (pointerIndex << MotionEvent.ACTION_POINTER_INDEX_SHIFT);
            } else if (action == MotionEvent.ACTION_DOWN) {
                action = MotionEvent.ACTION_POINTER_DOWN |
                        (pointerIndex << MotionEvent.ACTION_POINTER_INDEX_SHIFT);
            }
        }
        MotionEvent motionEvent = MotionEvent.obtain(lastTouchDown, now, action, pointerCount,
                pointerProperties, pointerCoords,
                0, 0, 1f, 1f,
                0, 0, source, 0);
        injectInputEvent(motionEvent);
    }

    private void injectInputEvent(MotionEvent motionEvent)  {
        try {
            // Set display ID for the motion event using reflection
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R && setDisplayIdMethod != null) {
                setDisplayIdMethod.invoke(motionEvent, displayId);
            }
            injectInputEventMethod.invoke(inputManager, motionEvent, displayId);
        } catch (IllegalAccessException | InvocationTargetException e) {
            Log.e("InputService", e.getMessage(), e);
        }
    }


    static {
        String methodName = "getInstance";
        Object[] objArr = new Object[0];
         try {
             Class<?> inputManagerClass;
             if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.UPSIDE_DOWN_CAKE) {
                inputManagerClass = Class.forName("android.hardware.input.InputManagerGlobal");
             } else {
                 inputManagerClass = InputManager.class;
             }

             inputManager = inputManagerClass.getDeclaredMethod(methodName)
                     .invoke(null, objArr);
             //Make MotionEvent.obtain() method accessible
             methodName = "obtain";
             MotionEvent.class.getDeclaredMethod(methodName)
                     .setAccessible(true);

             //Get the reference to injectInputEvent method
             methodName = "injectInputEvent";

             injectInputEventMethod = inputManagerClass.getMethod(methodName, InputEvent.class, Integer.TYPE);

             // Get the reference to setDisplayId method
             if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                 methodName = "setDisplayId";
                 setDisplayIdMethod = MotionEvent.class.getDeclaredMethod(methodName, Integer.TYPE);
                 setDisplayIdMethod.setAccessible(true);
             }

         } catch (Exception e) {
             Log.e("InputService", e.getMessage(), e);
         }
    }
}
