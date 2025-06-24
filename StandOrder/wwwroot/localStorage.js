window.standOrderStorage = {
    setTruckId: function (id) {
        localStorage.setItem("currentTruckId", id);
    },
    getTruckId: function () {
        return localStorage.getItem("currentTruckId");
    },
    clearTruckId: function () {
        localStorage.removeItem("currentTruckId");
    }
};
