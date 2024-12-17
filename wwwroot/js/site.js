// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Shows notification
function showNotification(status, message) {

    // Create a new notification element
    const notification = document.createElement('div');
    notification.classList.add('notification'); // Add base notification class
    notification.textContent = message;

    // Determine the notification type based on status code
    if (status >= 100 && status < 200) {
        notification.classList.add('info');
    } else if (status >= 200 && status < 300) {
        notification.classList.add('success');
    } else if (status >= 400 && status < 500) {
        notification.classList.add('warning');
    } else if (status >= 500) {
        notification.classList.add('error');
    }

    // Append the notification to the document body
    document.body.appendChild(notification);

    // Adjust position for stacking
    const notifications = document.querySelectorAll('.notification');
    notifications.forEach((notif, index) => {
        notif.style.bottom = `${110 + index * 60}px`; // Adjust stacking gap
    });

    // Remove the notification after 3 seconds
    setTimeout(() => {
        notification.classList.remove('info', 'success', 'warning', 'error', 'show');
        setTimeout(() => {
            notification.remove(); // Remove the element after fade-out
        }, 500); // Match CSS transition duration
    }, 3000);    
}

async function createCluster() {
    try {
        const select = document.getElementById('select-rancher-template');
        const selectedValue = select.value;
        const name = document.getElementById('clusterName');
        const clusterName = name.value;

        let isClasterCreated = false;

        var url = '/Provision/CreateClaster';
        var data = {
            RancherId: selectedValue,
            ClusterName: clusterName
        }

        const result = await postToController(url, data)
            .then(data => {
                showNotification(data.value.status, data.value.message);
                if (data.value.status === 200) {
                    isClasterCreated = true;
                }
            });

        if (true) {   //  (result.value.status === 200) {
            
            showNotification(105, 'Test cluster creation started.\n Used only for Proxmox Provision test');
            //await wait(5);

            const selectProxmox = document.getElementById('select-proxmox-template');
            const selectedProxmoxValue = selectProxmox.value;

            const amountOfWorkers = document.getElementById('workerNumber');
            const amount = amountOfWorkers.value;

            const template = document.getElementById('select-vm-template');
            const templateId = template.value;

            const etcdAndCPInput = document.getElementById('etcdAndCPNumber');
            const etcdCPAmount = etcdAndCPInput.value;

            const vmStartIndex = document.getElementById('vmStartIndex');
            const startIndexValue = vmStartIndex.value;

            const vmPrefixInput = document.getElementById('vmPrefixInput');
            let vmPrefixValue = vmPrefixInput.value;           

            const data = {
                etcdAndControlPlaneAmount: etcdCPAmount,
                workerAmount: amount,
                proxmoxId: selectedProxmoxValue,
                vmTemplateId: templateId,
                rancherId: selectedValue,
                clusterName: clusterName,
                vmStartIndex: startIndexValue,
                vmPrefix: vmPrefixValue,
            }            

            const clusterInfo = {
                rancherId: selectedValue,
                clusterName: clusterName,
            }

            const connString = await GetConnectionStringToRancher(clusterInfo);

            //const vmIDs = await startCreatingProxmoxVMs(data);  //   <--  Remove comment to start proxmox privision

            //if (connString !== '' && vmIDs.length > 0) {
            //    await StartVMsAndConnectToRuncher(connString, vmIDs);
            //}

            let vmIDArray = [100, 101];
            await StartVMsAndConnectToRuncher(connString, vmIDArray);
        }
    }
    catch (error) {
        console.error('Connection error:', error);
    }
}

async function postToController(url, data) {
    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });

        if (response.ok) {
            const result = await response.json(); // Если метод возвращает JSON
            console.log('Successful responce:', result);
            return result;
        } else {
            const result = await response.json(); // Если метод возвращает JSON
            console.error('Error while execution a request:', result);
            return result;            
        }
    } catch (error) {
        console.error('Connection error:', error);
    }
}

async function StartVMsAndConnectToRuncher(connString, vmIDs) {

    showNotification(100, 'Connection of VM`s started');

    const url = '/Provision/StartVMAndConnectToRancher';
    //const data = {
    //    connectionString: connString,
    //    vmsId: vmIDs
    //}

    const data = {
        connectionString: connString,
        vmsId: vmIDs
    }

    let res = await postToController(url, data)
        .then(result => {
            showNotification(result.value.status, result.value.message);
            return result.value;
        });
}

async function startCreatingProxmoxVMs(data) {

    showNotification(100, 'Creation of VM`s started');

    //await wait(5);

    const url = '/Provision/CreateProxmoxVMs';

    let vmIDs = await postToController(url, data)
        .then(data => {
            let result = [];

            // Проверка на существование данных и массива в свойстве "value"
            if (data && Array.isArray(data.value)) {
                data.value.forEach(item => {
                    if (item.status === 200) {
                        showNotification(item.status, 'Successfuly created VM: ' + item.message);
                        // Добавляем числовое значение из item.message в массив
                        let num = parseInt(item.message, 10);
                        if (!isNaN(num)) {
                            result.push(num);
                        }
                    }
                    else {
                        showNotification(item.status, item.message);
                    }             
                });
            }
            return result; // Return array of vmIDs
        });

    //let vmsId = await postToController(url, data)
    //    .then(data => {
    //        // Extract the array from the "value" property
    //        if (data && Array.isArray(data.value)) {
    //            data.value.forEach(item => {
    //                if (item.status === 200) {
    //                    showNotification(item.status, 'Successfuly created VM: ' + item.message);
    //                }
    //                else {
    //                    showNotification(item.status, item.message);
    //                }
    //            });
    //        }
    //    });

    return vmIDs;
}

async function GetConnectionStringToRancher(data) {

    const url = '/Provision/GetConnectionStringToRancher';

    let result = await postToController(url, data)
        .then(data => {
            // Extract the array from the "value" property
            if (data.value.status === 200) {
                return data.value.message;
            }            
        });

    return result;
}

async function getTemplates(url, selectTemplate, payload) {

    await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json', // Specify JSON content type
        },
        body: JSON.stringify(payload), // Convert the payload to a JSON string
        
    })
    .then(responce => {
        if (!responce.ok) {
            throw new Error('Network response was not ok');
        }
        return responce.json();
    })
    .then(data => {
        // Clean existing options
        const select = document.getElementById(selectTemplate);
        select.innerHTML = ''; // Clear existing options

        if (select) {
            select.innerHTML = ''; // Clear existing options

            const defaultOption = document.createElement('option');

            defaultOption.place
            defaultOption.value = '';
            defaultOption.textContent = 'Select item';
            select.appendChild(defaultOption);

            // Extract the array from the "value" property
            if (data && Array.isArray(data.value)) {
                data.value.forEach(item => {
                    const option = document.createElement('option');
                    option.value = item.value;
                    option.textContent = item.text;
                    select.appendChild(option);
                });
            } else {
                console.error('Response does not contain a valid "value" array:', data);
            }
        } else {
            console.error(`Select element with ID '${selectTemplate}' not found.`);
        }
    })
    .catch(error => {
        // Handle errors
        const select = document.getElementById(selectTemplate);
        // Ensure the select element exists
        if (select) {
            select.innerHTML = ''; // Clear existing options

            // Add an error message option
            const errorOption = document.createElement('option');
            errorOption.value = '';
            errorOption.textContent = 'Error while downloading data';
            select.appendChild(errorOption);
        } else {
            console.error(`Select element with ID '${selectTemplate}' not found.`);
        }

        console.error('There was a problem with the fetch operation:', error);
    });    
}

async function wait(seconds) {
    return new Promise(resolve => setTimeout(resolve, seconds * 1000));
}