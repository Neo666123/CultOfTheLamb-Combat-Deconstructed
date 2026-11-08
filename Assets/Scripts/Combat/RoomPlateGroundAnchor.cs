using UnityEngine;

namespace MIeMie.Combat
{
    // Keeps a world-space visual proxy on the currently tuned screenshot room plane.
    // Only Z is corrected; Rigidbody2D remains authoritative for X/Y movement.
    [DefaultExecutionOrder(1000)]
    public sealed class RoomPlateGroundAnchor : MonoBehaviour
    {
        private const string RoomPlateName = "Dungeon 1 Screenshot Room Plate";

        private Transform roomPlate;

        private void Awake()
        {
            FindRoomPlate();
            SnapNow();
        }

        private void LateUpdate()
        {
            SnapNow();
        }

        public void SnapNow()
        {
            if (roomPlate == null)
                FindRoomPlate();
            if (roomPlate == null)
                return;

            Vector3 normal = roomPlate.forward;
            if (Mathf.Abs(normal.z) < 0.0001f)
                return;

            Vector3 position = transform.position;
            Vector3 planePoint = roomPlate.position;
            position.z = planePoint.z -
                         (normal.x * (position.x - planePoint.x) +
                          normal.y * (position.y - planePoint.y)) / normal.z;
            transform.position = position;
        }

        private void FindRoomPlate()
        {
            GameObject plate = GameObject.Find(RoomPlateName);
            roomPlate = plate != null ? plate.transform : null;
        }
    }
}
