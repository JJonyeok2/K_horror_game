using System.Collections.Generic;
using UnityEngine;

namespace KHorrorGame.Migration
{
    public sealed class VanCargoHold : MonoBehaviour
    {
        [SerializeField] private Transform[] cargoSlots = new Transform[0];
        [SerializeField] private Transform fallbackSlot;
        [SerializeField] private Vector3 fallbackLocalOffset = new Vector3(0f, 0.25f, 0f);
        [SerializeField] private Vector3 fallbackSpacing = new Vector3(0.42f, 0f, 0.32f);

        private readonly List<VanCargoItem> cargoItems = new List<VanCargoItem>();

        public IReadOnlyList<VanCargoItem> CargoItems
        {
            get
            {
                RemoveMissingCargoItems();
                return cargoItems.ToArray();
            }
        }

        public int CargoCount
        {
            get
            {
                RemoveMissingCargoItems();
                return cargoItems.Count;
            }
        }

        public int TotalCargoValue
        {
            get
            {
                RemoveMissingCargoItems();
                var total = 0;
                foreach (var cargoItem in cargoItems)
                {
                    total += cargoItem.Value;
                }

                return total;
            }
        }

        public bool TryStore(ArtifactDefinition definition, out VanCargoItem cargoItem)
        {
            cargoItem = null;
            if (definition == null)
            {
                return false;
            }

            RemoveMissingCargoItems();

            var cargoObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cargoObject.name = "VanCargo_" + SafeName(definition.DisplayName);
            var targetParent = ResolveStorageParent(out var localPosition);
            cargoObject.transform.SetParent(targetParent, false);
            cargoObject.transform.localPosition = localPosition;
            cargoObject.transform.localRotation = Quaternion.identity;

            cargoObject.transform.localScale = definition.HandSlots >= 2
                ? new Vector3(0.65f, 0.42f, 0.42f)
                : new Vector3(0.32f, 0.24f, 0.24f);

            var cargoCollider = cargoObject.GetComponent<Collider>();
            if (cargoCollider != null)
            {
                DestroyCargoObject(cargoCollider);
            }

            cargoItem = cargoObject.AddComponent<VanCargoItem>();
            cargoItem.Initialize(definition, this);
            cargoItems.Add(cargoItem);
            return true;
        }

        public int ConsumeSettledCargo()
        {
            RemoveMissingCargoItems();
            var settledValue = TotalCargoValue;
            var cargoSnapshot = cargoItems.ToArray();
            cargoItems.Clear();

            foreach (var cargoItem in cargoSnapshot)
            {
                if (cargoItem != null)
                {
                    DestroyCargoObject(cargoItem.gameObject);
                }
            }

            return settledValue;
        }

        public int TransferCargoTo(VanCargoHold destination)
        {
            if (destination == null || destination == this)
            {
                return 0;
            }

            RemoveMissingCargoItems();
            var cargoSnapshot = cargoItems.ToArray();
            var transferredValue = 0;

            foreach (var cargoItem in cargoSnapshot)
            {
                if (cargoItem == null || cargoItem.Definition == null)
                {
                    continue;
                }

                if (!destination.TryStore(cargoItem.Definition, out _))
                {
                    continue;
                }

                transferredValue += cargoItem.Value;
                cargoItems.Remove(cargoItem);
                DestroyCargoObject(cargoItem.gameObject);
            }

            return transferredValue;
        }

        public void RegisterSlot(Transform slot)
        {
            if (slot == null || HasSlot(slot))
            {
                return;
            }

            var existingSlots = cargoSlots ?? new Transform[0];
            var slots = new Transform[existingSlots.Length + 1];
            for (var i = 0; i < existingSlots.Length; i++)
            {
                slots[i] = existingSlots[i];
            }

            slots[slots.Length - 1] = slot;
            cargoSlots = slots;
        }

        public void RegisterFallbackSlot(Transform slot)
        {
            if (slot != null)
            {
                fallbackSlot = slot;
            }
        }

        public bool RemoveCargo(VanCargoItem cargoItem)
        {
            if (cargoItem == null)
            {
                return false;
            }

            var removed = cargoItems.Remove(cargoItem);
            if (removed)
            {
                cargoItem.InitializeLoose(cargoItem.Definition);
                cargoItem.transform.SetParent(null, true);
            }

            return removed;
        }

        private Transform ResolveStorageParent(out Vector3 localPosition)
        {
            foreach (var slot in cargoSlots ?? new Transform[0])
            {
                if (slot != null && !IsSlotOccupied(slot))
                {
                    localPosition = Vector3.zero;
                    return slot;
                }
            }

            var offset = FallbackOffsetFor(FallbackCargoCount());
            if (fallbackSlot != null)
            {
                localPosition = offset;
                return fallbackSlot;
            }

            localPosition = fallbackLocalOffset + offset;
            return transform;
        }

        private bool IsSlotOccupied(Transform slot)
        {
            foreach (var cargoItem in cargoItems)
            {
                if (cargoItem != null && cargoItem.transform.parent == slot)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasSlot(Transform slot)
        {
            foreach (var existingSlot in cargoSlots ?? new Transform[0])
            {
                if (existingSlot == slot)
                {
                    return true;
                }
            }

            return false;
        }

        private int FallbackCargoCount()
        {
            var count = 0;
            foreach (var cargoItem in cargoItems)
            {
                if (cargoItem != null && !HasSlot(cargoItem.transform.parent))
                {
                    count += 1;
                }
            }

            return count;
        }

        private Vector3 FallbackOffsetFor(int index)
        {
            var column = index % 3;
            var row = index / 3;
            return new Vector3(fallbackSpacing.x * column, fallbackSpacing.y, fallbackSpacing.z * row);
        }

        private void RemoveMissingCargoItems()
        {
            for (var i = cargoItems.Count - 1; i >= 0; i--)
            {
                if (cargoItems[i] == null)
                {
                    cargoItems.RemoveAt(i);
                }
            }
        }

        private static string SafeName(string displayName)
        {
            return string.IsNullOrWhiteSpace(displayName)
                ? "Artifact"
                : displayName.Replace(' ', '_');
        }

        private static void DestroyCargoObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

            DestroyImmediate(target);
        }
    }
}
