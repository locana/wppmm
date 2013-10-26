using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPPMM.CameraManager
{
    class EvConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="definition"></param>
        /// <returns></returns>
        public static EVStepDefinition GetDefinition(int definition)
        {
            switch (definition)
            {
                case 1:
                    return EVStepDefinition.EV_1_3;
                default:
                    return EVStepDefinition.Undefined;
            }
        }

        public static int GetIntDefinition(EVStepDefinition definition)
        {
            switch (definition)
            {
                case EVStepDefinition.EV_1_3:
                    return 1;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index">Index of the EV for current step definition</param>
        /// <param name="definition">Current step definition</param>
        /// <returns>Exposure value to display.</returns>
        public static float GetEV(int index, int definition)
        {
            return GetEV(index, GetDefinition(definition));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index">Index of the EV for current step definition</param>
        /// <param name="definition">Current step definition</param>
        /// <returns>Exposure value to display</returns>
        public static float GetEV(int index, EVStepDefinition definition)
        {
            float by = 0;
            switch (definition)
            {
                case EVStepDefinition.EV_1_3:
                    by = 0.33f;
                    break;
                default:
                    return 0;
            }

            return (float)index * by;
        }
    }

    enum EVStepDefinition
    {
        Undefined,
        EV_1_3
    }
}
