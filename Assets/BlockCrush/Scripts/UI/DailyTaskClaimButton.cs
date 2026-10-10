using UnityEngine;
namespace BlockCrush.UI
{
    public sealed class DailyTaskClaimButton : MonoBehaviour
    {
        [SerializeField] DailyTasksController controller;
        [SerializeField] int index;
        public void Configure(DailyTasksController owner,int task) { controller=owner; index=task; }
        public void Claim() => controller.Claim(index);
    }
}
